# 2D Ball Catch Game - Testing Checklist

Legend: **Auto** = verified by script in the Unity Editor (Play mode); **Manual** = please check by hand.
"Stepped" means the physics was advanced manually (`Physics.Simulate`, 20 ms steps) over many attempts, so results are exact and repeatable.

## Acceptance criteria

| # | Criterion | Result | How it was verified |
|---|-----------|--------|---------------------|
| 1 | Ball spawns at top-center | Auto - pass | Ball position equals `TopCenterLaunchPoint` at menu, on Play, and at all 20 launches of the accelerated run |
| 2 | First target is RIGHT | Auto - pass | First launch after Play: target RIGHT, ball heads for the LEFT wall |
| 3 | RIGHT target: top-center -> left wall -> right target | Auto - pass | 200 stepped RIGHT attempts: wall contact at the left wall, landing X = planned X in 15.0-19.7 |
| 4 | LEFT target: top-center -> right wall -> left target | Auto - pass | 200 stepped LEFT attempts: wall contact at the right wall, landing X in 0.3-5.0 |
| 5 | Exactly one intended wall bounce per attempt | Auto - pass | 400 stepped attempts: bounce count = 1, no extra contacts on catches. A miss may brush the far wall *after* passing the target; that contact is ignored (no reflection) and the ball slides down it at constant speed |
| 6 | Target variation stays inside the correct zone | Auto - pass | Observed wall-hit height 3.4-7.95; target X 15.08-19.67 (RIGHT) and 0.32-5.00 (LEFT) |
| 7 | Player moves continuously while a direction key is held | Auto (logic) + Manual | Input mapping verified (D/Right = +1, A/Left = -1, none = 0, both = 0). Movement stepped at 6.9, 20, 33 and 50 ms physics steps: exactly 8.00 u/s each. A live key-hold passed in Phase 2; **please re-check with a real keyboard** |
| 8 | Player stops when the key is released | Auto - pass | Zero drift after release at every step rate |
| 9 | Player cannot leave the playable area | Auto - pass | Held 8 s each way: x = 19.100 and 0.900 exactly, Y and Z unchanged |
| 10 | Catch awards exactly one point | Auto - pass | 200 catches: +1 each; duplicate catch callback ignored |
| 11 | Miss awards zero points | Auto - pass | 200 misses: score unchanged |
| 12 | Miss does not trigger Game Over | Auto - pass | No Game Over state exists; accelerated run continued through all misses |
| 13 | Catch and miss both reset the ball | Auto - pass | Every launch of the accelerated run started at top-center; player also reset to start |
| 14 | Target switches after every attempt | Auto - pass | 20 launches alternate RIGHT, LEFT, RIGHT ... without exception |
| 15 | Score persists across attempts | Auto - pass | Score rose monotonically (+1 per catch only) |
| 16 | Restart resets score, target, player, ball | Auto - pass | Score 0, target RIGHT, Playing, ball relaunched from top-center, player at start; pending delay timer cancelled; 40 repeated restarts stable |
| 17 | Pause freezes ball, player, gameplay timers | Auto - pass | Ball position/velocity and player identical after 2 s paused; attempt-delay timer stayed frozen across a 3 s pause |
| 18 | Resume preserves the exact attempt | Auto - pass | Ball position, velocity, phase, bounce count and score identical at the instant of resume |
| 19 | Main Menu confirmation works | Auto - pass | Dialog text correct; Cancel returns to pause unchanged; Yes returns to Main Menu with a clean state |
| 20 | Music and SFX sliders independent, via mixer parameters | Auto - pass | 0.5 -> -6.02 dB, 0.25 -> -12.04 dB, 1.0 -> 0 dB, 0 -> -80 dB; each slider moves only its own parameter; measured output level of a test tone through the SFX group halves with each -6 dB step; AudioSource volumes untouched; works while time is frozen; values restored after restarting Play mode |
| 21 | No unexpected Z movement | Auto - pass | Z stayed 0 through 400 stepped attempts |
| 22 | No duplicate catch / miss / reset events | Auto - pass | Exactly one event per attempt, and no further events over 120 extra steps |
| 23 | No unresolved Console errors from the implementation | Auto - pass | 0 errors across all test sessions. One warning (CS1998) comes from the third-party `TwoUncleVFX/Common/Script/BulletController.cs` demo script |

## Extra checks done in Phase 7
- Camera framing: the whole play frame and the HUD strip are visible at 5:4, 4:3, 16:10, 16:9 and 21:9 (`CameraFit`).
- Build settings now list only `GameplayScene` (it was opening an empty `SampleScene` first).
- Ball speed is constant for the whole attempt, including a missed ball that touches the far wall.

## Manual tests (not possible to confirm from the automation)
1. **Real keyboard**: press Play, hold A / Left Arrow and D / Right Arrow; the paddle moves smoothly while held and stops on release.
2. **Escape**: during play press Esc (pauses), Esc again (resumes); from Settings or the confirmation dialog Esc goes back one step. Logic verified in the Editor; a real key press was not.
3. **Mouse**: click every button (Play, Settings, Exit, Pause, Resume, Restart, Main Menu, Yes, Cancel, Back) and drag both sliders.
4. **Audio**: assign clips on the `Audio` object (see `SetupGuide.md`), then check music and effects respond to their own slider, that muting one leaves the other, and that sounds still play while paused.
5. **Look and feel**: watch several attempts for the trail, sparkles and the hop after a catch; adjust in `BallFeedback` and the Ball prefab if needed.
6. **Standalone build**: File > Build Profiles > Windows, build, run; confirm the menu appears first and Exit quits.
7. **Window sizes**: resize the Game view / built window to a few resolutions and check nothing is clipped.
