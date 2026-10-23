# 2D Ball Catch Game - Setup Guide

Unity 6000.3.10f1, URP, Input System package (new Input System only).

## Running the game
1. Open `Assets/_Game/Scenes/GameplayScene.unity`.
2. Press Play. The Main Menu appears; **Play** starts a session. Controls: hold **A / Left Arrow** or **D / Right Arrow**; **Esc** pauses and resumes.

## Audio (Audio Mixer)
The mixer asset is `Assets/_Game/Settings/GameAudioMixer.mixer`.

| Group  | Parent | Exposed parameter | Used for |
|--------|--------|-------------------|----------|
| Master | -      | -                 | everything |
| Music  | Master | `MusicVolume`     | background music |
| SFX    | Master | `SFXVolume`       | gameplay and UI sound effects |

The Settings sliders drive the exposed parameters through `SettingsManager` (linear 0..1 -> decibels, `20 * log10(v)`, 0 maps to -80 dB). AudioSource volumes are never changed. Values are saved in PlayerPrefs (`settings.musicVolume`, `settings.sfxVolume`, default 0.8).

### Adding your own sounds
No audio clips ship with the project. Select the **Audio** object in the scene and assign clips on the `AudioManager` component; empty slots simply play nothing:
- **Music Clip** - looped background music (plays through `MusicSource`, routed to Music).
- **Ui Click Clip** - played on every UI button press.
- **Wall Bounce Clip** - the single wall bounce of each attempt.
- **Catch Clip** / **Miss Clip** - played when an attempt resolves.

Put clips under `Assets/_Game/Audio/Music` and `Assets/_Game/Audio/SFX`. Both AudioSources (`MusicSource`, `SfxSource`) already have their **Output** set to the Music and SFX mixer groups; AudioManager re-applies this routing at startup.

### If the mixer needs rebuilding by hand
1. Window > Audio > Audio Mixer, create `GameAudioMixer`, add child groups **Music** and **SFX** under Master.
2. For each of Music and SFX: select the group, right-click the **Volume** label in the Inspector > *Expose 'Volume' to script*.
3. In the Exposed Parameters list rename them to exactly `MusicVolume` and `SFXVolume`.
4. Assign the mixer to `SettingsManager` and the two groups to `AudioManager`.

## Scene objects
- **GameManager** - state, score, target side. Leave *Start Session On Play* off so the menu starts the game.
- **PlayArea** - `PlayAreaBounds`: 20 x 10 play area, target zones LEFT 0-5 and RIGHT 15-20.
- **Player**, **Ball** - prefabs in `Assets/_Game/Prefabs`.
- **UI** - canvas with Main Menu, HUD, Pause, Settings and Confirm panels; one is visible at a time.
- **Audio**, **SettingsManager** - audio playback and volume preferences.
