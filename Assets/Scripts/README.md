# Mouse rhythm prototype

## Script folders

| Folder | Contains |
| --- | --- |
| Gameplay | Session controller, chart rules, note rendering, holds, clock and leg tracker |
| Selection | Song browser, mode/height/difficulty choices and animated mode previews |
| VisualEffects | Incoming sparks, hit particles, ripples, space themes and background motion |
| UI | Text entrances, hover feedback, judgement/score animation, results and history presenters |
| Audio | Gameplay sounds, screen music, click sounds and hit-sound timing |
| Settings | Audio settings, rhythm calibration/preferences and sensor settings framework |
| Data | Local JSON settings, session history storage and session summary data |
| Wellbeing | Pre-session assessment and wellbeing session state |
| Navigation | Scene buttons and iris transitions |
| Editor | Editor-only helpers; excluded from game builds |

The iris shader is in `Assets/Shaders/IrisTransition.shader`. Script class names and asset GUIDs are unchanged, so existing Inspector and button references continue to work.

Open `Assets/Scenes/MainMenu.unity`, enter Play mode and choose START.

- Move the mouse vertically inside the playfield to control the marker height.
- Press Space when a note meets the judgement line while holding the matching height.
- Holding a height is allowed; each note still requires a separate Space press.
- Escape or PAUSE opens the pause panel. Focus loss pauses the session; resume explicitly.
- START opens Song Selection, then Game Mode Selection. Therapy goes to Exercise Selection;
  Free goes to Difficulty Selection. Both then enter Gameplay.
- Eight placeholder songs provide different generated metronome tempos. They have no recorded
  song audio yet. Edit their names, artists, BPM and cover colors in `Assets/Data/PlaceholderSongs.asset`.
- Therapy uses 32 notes at the chosen LOW/MEDIUM/HIGH level and a 1.25× timing window.
- Free notes can appear anywhere between the LOW/MID/HIGH guide lines. Easy uses 16 notes in
  the lower half with gentler changes and a 1.3× timing window. Medium uses 24 notes across the
  full range and the standard window; Hard uses 32 with larger height changes and a 0.8× window.
- Free Easy includes exact 25% halfway targets; Medium and Hard include both 25% and 75%
  halfway targets, alongside other intermediate heights.
- The chart begins after eight beats of lead-in. Replay and restart retain the chosen song/mode.
- Pause, restart, end-session/results, replay and return-to-menu actions are wired in the Inspector.

## Scene authoring

Scenes are MainMenu, SongSelection, ModeSelection, ExerciseSelection, DifficultySelection,
Gameplay, Settings, RhythmCalibration, SensorCalibration, SensorConnection, Results and
PerformanceHistory. UI, note slots, audio-source objects,
and buttons are saved scene objects. Runtime scripts move existing note objects and update labels;
they do not construct the interface. The chart and reference fields are editable on the Controllers
object's PrototypeGameplay component. Lane indices are 0 LOW, 1 MID, 2 HIGH. For intermediate
notes enable useCustomHeight and set customHeight from 0 (LOW) to 1 (HIGH), with .5 at MID.
Rendering, colors and hit judgement all use the same TargetHeight. Existing authored charts keep
their lane values. Free demo charts use repeatable fractional heights; Therapy stays fixed.
Session selections
replace the fallback chart at runtime; the existing 32 note slots are reused. The song browser uses
an authored horizontal ScrollRect with eight cards, text search, a tempo filter, and browse arrows.
To add catalog entries, duplicate a saved song card, set its SongCardView index, and add it to the
SongSelectionController cards array. There must be one authored card per catalog entry.

## Scoring and limitations

Standard timing windows are ±80 ms Perfect, ±140 ms Great, and ±220 ms Good, multiplied by the
selected mode/difficulty scale. Height tolerance is 0.18 in Therapy/Easy, 0.14 in Medium and 0.11
in Hard. Heights are normalized mouse positions, not measured physical angles. Wrong-height
presses within a note's timing window consume it as Miss; pressing
outside every note's timing window is ignored. Accuracy weights are Perfect 1, Great .8, Good .5,
Miss 0. Scores are 1000, 700, 400, 0. Completion is judged notes divided by chart notes, including
misses. Duration excludes pauses and is capped at chart end.

Beat and hit audio volumes persist in PlayerPrefs. There is no Bluetooth firmware, sensor
calibration, physical-angle/ROM measurement, recorded song audio, or medical recommendation.
Results explicitly identify mouse mode and do not invent sensor data.

## Scene transitions

All scene-navigation buttons use IrisSceneTransition.Load: a circular close (.22 seconds),
asynchronous scene load while covered, and circular open (.28 seconds). The animation uses
unscaled time and blocks clicks until it finishes. Gameplay starts its beat countdown after
the iris opens. Restart/Home stop the departing gameplay audio.

Every scene has the shared `Assets/UI/Transitions/SceneTransition.prefab`, containing a saved
Canvas and IrisOverlay RawImage. One instance persists between scenes; new scene copies remove
themselves. Edit Close Duration and Open Duration on the prefab's IrisSceneTransition component
to adjust all scenes. The material controls cover color and edge softness; runtime material
changes do not modify that asset. No interface objects are created at runtime.

## Settings and calibration

Settings keeps the existing automatically saved beat/SFX volumes and links to three pages.
RhythmCalibration offers a looping 120 BPM mouse-and-Space preview, standard judgement windows,
and fixed LOW/MID/HIGH notes. Judgement offset ranges from -250 to +250 ms: positive accepts
a later press, negative an earlier press. Note speed ranges from 0.5x to 2x; it scales approach
travel time without changing BPM, beat timestamps, difficulty windows, or height requirements.

Slider changes affect the preview immediately. SAVE persists both values to PlayerPrefs and
future gameplay reads them at session start. RESET VALUES resets the draft to 0 ms/1x; SAVE
is still required to apply that reset. Leaving without saving discards draft changes. Preview
practice is never added to session history and stops on navigation or focus loss.

SensorConnection and SensorCalibration are framework-only. Device name, battery and signal
remain unavailable; scan reports that hardware integration is pending. Connect, Calibrate and
Next controls are disabled. No Bluetooth link, calibration progress, reference angle or ROM is
simulated. All panels, note slots, controls and navigation callbacks are saved scene objects.

Calibration checks: Tools/VerifyRhythmPreferences.cs and Tools/test-calibration-settings.cjs.
The live checks restore the original rhythm preferences and do not save test sessions.

## Profile and history

PROFILE opens PerformanceHistory. Sessions are saved when they finish automatically or when
END SESSION opens Results. Restart and return-to-menu abandon the current attempt. Earlier
sessions from before this feature cannot be recovered. Records persist as session-history.json
under Application.persistentDataPath on this computer. Each record has a unique ID to prevent
duplicate saves; unreadable history is preserved instead of overwritten.

The graph shows the last eight sessions. Overall accuracy uses the existing judgement weights;
score is the actual session score. Movement Accuracy is mouse height-match rate among hit
attempts within a note's timing window, not physical movement accuracy. Average ROM remains
unavailable until calibrated sensor measurements exist. Early-ended results are labeled explicitly.
VIEW ALL uses a vertical scroll list with ten saved row objects and pages of ten sessions.
All graph points, segments, labels and buttons are authored scene objects editable in the Inspector.
Persistence checks: Tools/VerifySessionHistory.cs, entry VerifySessionHistory.Main (isolated files).

Core scoring checks can be run through Unity CLI's `script run` using
`Tools/VerifyRhythmPrototype.cs` (entry point `VerifyRhythmPrototype.Main`).
Selection/chart checks use `Tools/VerifySelectionFlow.cs` (entry point `VerifySelectionFlow.Main`).
