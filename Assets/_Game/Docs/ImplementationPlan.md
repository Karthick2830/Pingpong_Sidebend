# 2D Ball Catch Game - Implementation Plan & Checklist

Unity 6000.3.10f1, URP 17.3, Input System 1.18 (active handler = Input System only), uGUI 2.0.
Specification: `Docs/2D_Ball_Catch_Game_Functional_Specification.docx`.

## Phases
- [x] Phase 0 - Audit and plan
- [x] Phase 1 - Scene, camera, walls, ground, player, ball (GameplayScene, prefabs, PlayAreaBounds)
- [x] Phase 2 - Player movement (Keyboard.current, A/D + arrows)
- [x] Phase 3 - Ball trajectory (single wall bounce, variation)
- [x] Phase 4 - Catch / miss / score / alternation / reset
- [x] Phase 5 - Game states and UI
- [x] Phase 6 - Audio mixer and settings
- [x] Phase 7 - Validation, docs (SetupGuide.md, TestingChecklist.md)

## Architecture
GameManager (state, score, target side, attempt flow), GameState enum, BallController, PlayerController,
PlayAreaBounds, UIManager (+ MainMenuUI, PauseMenuUI, SettingsUI), AudioManager, SettingsManager.

## Phase 1 notes
- Play area inner rectangle is X 0..20, Y 0..10 (widened by user request; spec target ranges scaled to LEFT 0-5, RIGHT 15-20); walls sit outside it.
- Ground is a trigger (miss, never a bounce). Ball: Rigidbody, no gravity, ContinuousDynamic, Z frozen.
- Player: kinematic Rigidbody, Z frozen, box collider.
- Orthographic camera at (10, 5, -10), size 6.2. HUD/UI goes in the screen margin above the top wall (Phase 5).


## Phase 3 notes
- BallController picks a wall point and target point per attempt, launches at the wall, and on the one intended wall contact flips the horizontal direction and redirects to the target at constant speed (pure mirror reflection would lock wall height to target X and remove variation).
- Walls are identified with WallSurface (Left/Right). Extra or wrong-wall contacts are ignored and counted in IgnoredWallContacts.
- Verified by stepping physics manually over 40 alternating attempts (0 failures).

## Phase 4 notes
- GameManager owns state, score, target side. First catch/miss moves state Playing -> AttemptTransition, so duplicate callbacks are ignored; next attempt starts after attemptResetDelay (scaled time).
- BallController raises PlayerHit / GroundHit once per attempt (then phase = Resolved). Ground is a trigger marked with GroundSurface.
- startSessionOnPlay is on until the Phase 5 menu drives StartNewSession.
- Verified: 200 stepped attempts (0 failures) plus real-time catch, miss, alternation and restart (mid-flight and during the delay).

## Feel pass (after Phase 4)
- Player resets to start together with the ball after every attempt (GameManager.StartNextAttempt).
- Ball root keeps collider/Rigidbody; mesh is a child 'Visual' so BallFeedback can squash/stretch it. Trail renderer is cleared whenever the ball is repositioned.
- On catch the ball hops off the player (visual only, gravity on) and rests at the ground if it falls there; reset delay is 0.9s so the hop can play out. Misses stop on the ground (no bounce, per spec).
- Gameplay rules unchanged: constant-speed straight paths, one wall bounce.
- Trail pass: two additive trails (violet glow + pink-white core), shed sparks while flying, directional spark spray on wall/paddle/ground impacts (BallFeedback; effects are children of the Ball prefab).
- Note: a missed ball can keep travelling past the target and touch the far wall before the ground; that contact is ignored (no reflection) and counted in IgnoredWallContacts.

## Phase 5 notes
- Single GameplayScene with one UI canvas; panels (MainMenu, HUD, Pause, Settings, Confirm) are shown one at a time by UIManager from GameManager.StateChanged. HUD sits in the strip above the top wall (camera: size 6.6 at (10, 5.48)).
- GameState: MainMenu, Playing, AttemptTransition, Paused, Settings, ConfirmReturnToMenu. Frozen states (Paused, Settings, Confirm) set Time.timeScale = 0; player input is only enabled in Playing/AttemptTransition.
- Escape toggles pause / closes settings / cancels the confirm dialog. startSessionOnPlay is off; Play button starts the session.
- Settings sliders exist but are wired to the mixer in Phase 6.
- Trail (final): main trail uses a copy of TwoUncleVFX KikokenTrail.mat (Assets/_Game/Materials/BallTrailKikoken.mat), head 0.5 wide (ball is 0.6) tapering to a thin tail, no end cap so it starts at the ball; plus a thin bright core trail, shed sparks and impact spray. The original TwoUncleVFX assets are untouched; its Kikoken.vfx needs the VFX Graph package, which is not installed.
- Sparkles: generated four-point star texture (Assets/_Game/Materials/SparkleStar.png) drives TrailSparkles (twinkling while flying) and ImpactSparkle (burst on every impact), both wired through BallFeedback.

## Phase 6 notes
- GameAudioMixer (Master > Music, SFX) with exposed MusicVolume / SFXVolume. SettingsManager converts slider 0..1 to dB (zero = -80 dB), saves to PlayerPrefs; AudioManager routes sources and plays optional clips (none shipped). See SetupGuide.md.

## Phase 7 notes
- Fixed: build list opened SampleScene first (now GameplayScene only); play frame clipped at narrow aspects (new CameraFit); ball speed dropped when a miss scraped the far wall (now slides at constant speed).
- Deviations from the prompt's folder layout: one GameplayScene instead of a separate MainMenuScene (panels switch by game state); play area widened to 20x10 by request with proportionally scaled target zones.
- Results and manual steps: TestingChecklist.md. Audio setup: SetupGuide.md.

## After adding MediaPipe
- Compile errors (CS1061 'EventSystem.uiToolkitInterop' in com.unity.ugui WorldDocumentRaycaster) were caused by a stale cached copy of com.unity.ugui in Library/PackageCache that differed from the Editor's bundled ugui 2.0.0. Fixed by deleting only that cache folder and running a package resolve (Unity re-extracted the correct copy). If it ever returns: close Unity, delete Library/PackageCache/com.unity.ugui@*, reopen.
- MediaPipe (Packages/com.github.homuler.mediapipe 0.16.3 and Assets/MediaPipeUnity) was left untouched. Remaining warnings are from third-party code (MediaPipe sample ImageSource.cs obsolete Resolution.refreshRate; TwoUncleVFX BulletController.cs CS1998).
