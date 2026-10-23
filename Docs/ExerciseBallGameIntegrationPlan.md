# Exercise-Validated Ball Catch — Integration Plan (Phase A Audit)

Status: **Phase A only. No gameplay code, scene, or asset has been modified.** Everything below was read from the project source; nothing was run in the Unity Editor.

## 1. Project overview

- Unity **6000.3.10f1**; Input System package (`Keyboard.current`); uGUI (legacy `Text`) in the game, TMP in the tracking scene.
- Git repo, 2 commits; ball game added in "Add 2D Ball Catch Game Unity project".
- **Build Settings contain only** `Assets/_Game/Scenes/GameplayScene.unity`. `Assets/Core/MyScenes/ExerciseTracking.unity` is **not** in the build list.

| Area | Location | Namespace |
|---|---|---|
| Ball game | `Assets/_Game/Scripts/{Core,Gameplay,UI,Audio,Settings}` | `BallCatch.*` |
| Exercise core | `Assets/Core/CoreScripts/{Pose,Exercises,UI}` | `ExerciseGame.Core.*`, `ExerciseGame.Exercises.*` |
| MediaPipe runner (feeds pose) | `Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/PoseLandmarkerRunner.cs` | — |

Only `PoseLandmarkerRunner` (sample code) references `PoseDataProvider` outside CoreScripts.

## 2. Ball game architecture (`GameplayScene`)

Scene objects: `GameManager`, `PlayArea` (PlayAreaBounds), Player (PlayerController + kinematic Rigidbody), Ball (BallController), `Walls` (Left/Right/Top), `Ground`, `UI` (MainMenuPanel, HUDPanel with `ScoreText` + `PauseButton`, PausePanel, SettingsPanel, ConfirmPanel), `SettingsManager`, audio sources.

- **GameState**: MainMenu, Playing, AttemptTransition, Paused, Settings, ConfirmReturnToMenu.
- **Pause** = `Time.timeScale = 0` set in `GameManager.ApplyState` (frozen for Paused/Settings/ConfirmReturnToMenu). `OnDisable` restores 1.
- **PlayerController** (`Gameplay/PlayerController.cs`): `Update` sets `horizontalInput = inputEnabled ? ReadHorizontalInput() : 0` (A/D/arrows via `Keyboard.current`); `FixedUpdate` does `MovePosition` with `x += input * moveSpeed(8) * fixedDeltaTime`, clamped to `PlayAreaBounds.PlayerMinX..PlayerMaxX` (0.9..9.1), Y/Z pinned. `SetInputEnabled(bool)` is called by `GameManager.ApplyState` (true only in Playing/AttemptTransition). `ResetToStart()` zeroes input and teleports `body.position`/`transform.position` to `PlayerStartPosition` (X=5, Y=0.6).
- **Axis**: world X; **−1 = left, +1 = right** (matches `ReadHorizontalInput`).
- **BallController**: velocity-only; `Park()`, `Launch(TargetSide)`; events `PlayerHit`, `GroundHit`, `WallBounced`, `Parked`, `Launched`. One intended wall bounce enforced via `IntendedWall`/`WallBounceCount`.

### 2.1 Exact existing per-turn reset flow (MUST be preserved)

Source: `GameManager.cs`.

1. Ball raises `PlayerHit` (catch, `OnCollisionEnter`) or `GroundHit` (miss, `OnTriggerEnter` on `GroundSurface`). Both call `Resolve()` first so duplicates are ignored.
2. `GameManager.ResolveAttempt(result)` — guarded by `State == Playing`:
   1. `SetState(AttemptTransition)` (input stays enabled; HUD stays visible)
   2. Catch → `Score++`, `ScoreChanged`
   3. `CurrentTarget` toggles Right↔Left
   4. `AttemptResolved?.Invoke(result)`
   5. `pendingTransition = StartCoroutine(StartNextAttempt())`
3. `StartNextAttempt()`: `yield return new WaitForSeconds(attemptResetDelay = 0.9f)` (**scaled time**), then in this order:
   `player.ResetToStart()` → `ball.Park()` → `SetState(Playing)` → `ball.Launch(CurrentTarget)`.
4. Other resets (separate, must stay separate):
   - **Full restart**: `StartNewSession()` / `Restart()` → score 0, target Right, `player.ResetToStart()`, `ball.Park()`, state Playing, `ball.Launch`.
   - **Return to menu**: `ConfirmReturnToMainMenu()` → score 0, player reset, ball park.
   - Player also resets in `PlayerController.Awake`.

**Observation (important for the bridge):** the player keeps moving during the 0.9 s `AttemptTransition` (input enabled), so the player can still be driven after the catch/miss and is then teleported to center by `ResetToStart()`. `ResetToStart()` already zeroes `horizontalInput`, but any externally-held command would reassert movement on the next `Update`. The bridge therefore must clear its command when the reset occurs.

## 3. Exercise system (existing, to be reused)

### 3.1 Classes found (names match the brief)
`IExerciseValidator`, `ExerciseState`, `ValidationResult`, `PoseDataProvider`, `PoseFrame`, `SideBendValidator`, `SideBendSettings`, `ExerciseManager`, `ExerciseValidationTester`, `ExerciseUIDataProvider`, `ExerciseUIController`. Differences from the brief's wording are noted below.

- `IExerciseValidator`: `Reset()`, `Validate(PoseFrame, float dt)`, `CurrentState`, `IsRepCompleted`, `ConsumeRep()`.
- `ExerciseState`: Waiting, Ready, Moving, TargetReached, Holding, Returning, RepConfirmed, Invalid.
- `ValidationResult` is a **struct**: `isValid, quality, confidence, feedback`.
- `PoseDataProvider`: singleton `Instance` (**destroys duplicates in Awake**), `CurrentPose` (smoothed), `HasPose`, `CurrentTrackingQuality`, `IsTrackingReliable()`.

### 3.2 Pose data flow
MediaPipe `PoseLandmarkerRunner.UpdatePoseData` → `PoseDataProvider.UpdatePose` (smoothing + quality) → `ExerciseValidationTester.Update` reads `poseProvider.CurrentPose` and **calls `sideBendValidator.Validate(pose, Time.deltaTime)` once per rendered frame** → `ExerciseManager.Update` polls `validator.IsRepCompleted`, `currentRep++`, `ConsumeRep()`.

Notes:
- `SideBendValidator` has no `Update`; it is driven only by the Tester. The Tester is the one assigned to `ExerciseManager.validationTesterComponent` (it implements `IExerciseValidator` and forwards to `SideBendValidator`).
- `Validate()` sets `IsRepCompleted = false` at its start, then true on the frame the rep is confirmed. `ExerciseManager.Update` and the Tester each run in Update; correctness depends on `ExerciseManager` seeing the flag in the same frame (order is not explicitly set — see risks).
- Validation uses `deltaTime = Time.deltaTime`; with `timeScale = 0` this is 0, so hold/neutral timers freeze (no rep can complete while paused — acceptable, but the Tester keeps running during pause unless paused via `PauseValidation`).

### 3.3 SideBendValidator live state (candidate bridge inputs)
Public, already-computed: `CurrentState`, `IsRepCompleted`, `CurrentTorsoAngle` (sign-corrected), `DetectedDirection`, `CommittedDirection`, `ExpectedDirection`, `CompletedReps`, `IsPoseReady`, `CurrentReadinessState`. **Direction ints: −1 = LEFT, +1 = RIGHT, 0 = none** (confirmed in `DirectionToString` and `GetDirection`).
- `DetectedDirection` is computed every valid frame from the corrected angle; it is **not** gated by validity of the whole attempt.
- `CommittedDirection` is set only after the movement-stability checks pass and is zeroed on cancel/rep completion (`CancelAttempt`, Reset) — this is the better "validated live direction" for movement.
- On unready/untrackable pose, `Validate` calls `CancelAttempt()` and sets `CurrentState = Waiting`, returning `ValidationResult.Invalid(feedback)`.
- `invertDirection = true` (validator + Tester) so physical LEFT is negative. Direction mapping must be verified live.

**Gap:** there is no single "validated live movement direction" property. Proposed minimal, backward-compatible addition (Phase B): a read-only `int ValidatedMoveDirection` (or reuse `CommittedDirection` combined with `CurrentState`/`ExpectedDirection`) on `SideBendValidator`, computed from existing fields only, with no extra `Validate()` call. To be decided/confirmed when implementing; the exact states for which movement is allowed must be taken from the state machine (Moving/TargetReached/Holding), not guessed.

### 3.4 Reps & sets — current vs required
| Item | Current | Required |
|---|---|---|
| `SideBendSettings.asset` | `repMode = AlternateSides (1)`, `startingSide = Left (0)`, `direction = Both` | already correct for LEFT-first alternation |
| `ExerciseManager` (scene values) | `targetRepsPerSet = 4`, `targetSets = 2`, `startAutomatically = true` | 10 reps/set; set count not specified by brief (keep 2, confirm) |
| Per-side counts | **Not tracked** (`ExerciseManager` has only `currentRep`/`currentSet`) | 5 LEFT / 5 RIGHT per set |
| Next-set start side | `StartNextSet()` calls `validator.Reset()` → `completedReps = 0` → `UpdateExpectedDirection()` ⇒ expects LEFT | correct |
| Rep direction | Manager does not know which side a rep was | needs the side (see below) |

Plan: derive side from the alternating rule (`expectedDirection` immediately before confirmation) or expose `LastCompletedDirection` on the validator; track `leftReps`/`rightReps` in `ExerciseManager` (small additive properties). Because alternation is enforced by the validator, 10 reps/set ⇒ 5/5 by construction; the per-side counters exist for UI and verification. Duplicate-count protection already exists (`ConsumeRep` + `IsRepCompleted` cleared per `Validate`).

## 4. Exercise Tracking scene
Objects: `System`, `Solution` (MediaPipe runner), `Manager` (ExerciseManager), `Validator` (SideBendValidator), `Tester` (ExerciseValidationTester), `ExerciseUIDataProvider`, `UIController`, `ExerciseUIDataDebug`, Main Camera, EventSystem, pause panel (PAUSE/RESUME/RESTART/ENDWORKOUT). Pose provider, runner and camera live in this scene; nothing uses `DontDestroyOnLoad` or `SceneManager` (searched `_Game` and `Core`). So **there is currently no scene-transition flow** and no persistence pattern.

## 5. Integration architecture decision (needs user confirmation before Phase B)

The camera/MediaPipe runner, `PoseDataProvider`, validator, tester and manager exist only in `ExerciseTracking`. The ball game is a separate scene. Options:

- **A (recommended): additive scene load.** `GameplayScene` stays the single-active scene; `ExerciseTracking` is loaded additively (and its UI/camera disabled for gameplay), keeping all existing components untouched. The bridge finds the validator/manager through serialized references set at load or one cached lookup (no per-frame `Find`). Requires adding `ExerciseTracking` to Build Settings and conflict handling for duplicate Main Camera / EventSystem / AudioListener.
- B: copy exercise objects into `GameplayScene` (duplicates the brief's "no duplicate" rule risk; prefab-izing needed).
- C: single scene merge — highest regression risk.

I will not start any of these without your answer.

## 6. Proposed implementation (not started)

| Phase | Change | Files (planned) |
|---|---|---|
| B | `ExerciseBallMovementBridge` (new, in `_Game/Scripts/Gameplay`); small additive entry point on `PlayerController` (e.g. `SetExternalMoveDirection(float)`) and a development-only flag to disable keyboard in normal play; additive read-only direction property on `SideBendValidator` | new bridge; `PlayerController`; `SideBendValidator` (property only) |
| B (reset safety) | Bridge subscribes to `GameManager.AttemptResolved`/state changes (read-only) to zero its command; the existing reset coroutine is **not edited** | none in `GameManager` ideally |
| C | `ExerciseManager`: 10 reps, per-side counters, additive; scene value change `targetRepsPerSet` 4 → 10 | `ExerciseManager`, scene value |
| D | Minimal HUD text lines inside `HUDPanel` (set, reps, left/right, expected, feedback, completion) in the existing style, next to `ScoreText` | `UIManager` (small additive refs) or a new small HUD script |
| E | Scene loading, camera/EventSystem/AudioListener de-duplication | scene edits, build settings |
| F | Compile check, write-up | docs |

## 7. Risks / open issues
1. **Scene architecture undecided** (Section 5); `PoseDataProvider` self-destructs duplicates, so loading a second copy would silently disable one.
2. **Pause mismatch:** game pause freezes via `timeScale`; `ExerciseManager.PauseExercise` exists but nothing calls it from the game. The Tester keeps validating while paused. Bridge must also zero movement when `GameManager.State != Playing/AttemptTransition`.
3. **Timescale in validation:** `deltaTime = 0` during pause stalls timers; safe, but resume could hold a stale half-finished attempt. Consider `Reset` vs. keep (decide in Phase B).
4. **Update-order dependency** between Tester (`Validate`) and `ExerciseManager` (`IsRepCompleted` poll) — same-frame detection is currently order-dependent; verify before relying on it.
5. **Player moves during the 0.9 s `AttemptTransition`**; command must be cleared at reset (Section 2.1).
6. **Direction sign** relies on `invertDirection = true` and camera mirroring; needs live verification with a tracked body.
7. `ExerciseManager.PauseExercise/ResumeExercise/EndExercise` use `SendMessage` (existing; not to be copied for new code).
8. `ExerciseManager.startAutomatically = true` — if loaded additively it would start counting reps in the menu before the game begins; needs to be started/paused by game state.
9. Restart semantics: today `StartNewSession` resets score/ball/player only. Whether exercise progress should reset on restart is **undecided**; documented, not assumed.

## 8. Manual Inspector setup anticipated
Bridge: references to `PlayerController`, `GameManager`, `SideBendValidator` (and `ExerciseManager`); `ExerciseManager` target reps = 10; `ExerciseBallMovementBridge` keyboard-debug toggle off; HUD text references. Exact list will be finalized per phase.

## 9. Tests
None performed in Phase A (read-only audit). Phase B–F items needing a live camera/tracked body/Editor will be labelled as such and not reported as passed unless executed.

## 10. Implementation status (Phases B–E, first pass)

Decision taken: tracking scene is loaded **additively** at runtime by the bridge (no exercise objects copied into `GameplayScene`; `ExerciseTracking` scene file untouched). `ExerciseTracking` added to Build Settings.

| File | Change |
|---|---|
| `_Game/Scripts/Gameplay/ExerciseBallMovementBridge.cs` | NEW. Loads/links tracking scene, disables its camera/light/EventSystem, hides its canvas (alpha 0), maps `SideBendValidator.ValidatedMoveDirection` to the player, pauses/resumes `ExerciseManager` with game state, counts LEFT/RIGHT per set, clears its command on `BallController.Parked`, and stays stopped until the validator returns to neutral/zero. Stops on stale pose (no new frame for 0.5 s real time). |
| `_Game/Scripts/UI/ExerciseProgressHUD.cs` | NEW. Read-only text: set, reps, left/right, next side, feedback, completion. |
| `_Game/Scripts/Gameplay/PlayerController.cs` | `SetExternalMoveDirection`, keyboard behind `allowKeyboardInput` (default off); `ResetToStart` also clears the external command. Timing/position of the reset unchanged. |
| `Core/.../SideBendValidator.cs` | Added `ValidatedMoveDirection` and `RepConfirmed` event (read-only/additive). |
| `ExerciseManager.cs`, `ExerciseUIDataProvider.cs` | Added `FormerlySerializedAs("validationTester")`: the committed tracking scene stored the old field name, so both references loaded empty (pre-existing defect). |
| `GameplayScene` | Added `ExerciseBridge` (bridge + HUD) and `ExerciseProgressText` under `HUDPanel`; references assigned. `GameManager` untouched. |

Restart semantics chosen: exercise progress is kept across turn resets, pause and Restart; it is reset on return to Main Menu. Open question for the owner.

Verified in Editor Play mode (no camera/body): compiles, tracking scene loads additively, bridge connects, one PoseDataProvider, one AudioListener, no errors. **Not verified** (needs camera + tracked body): movement direction mapping, rep/set counting, per-side counts, stale-pose stop, pause/resume with live tracking.
