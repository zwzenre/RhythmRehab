# RhythmRehab

A Unity rehabilitation rhythm-game prototype with a space theme. Players match incoming notes using mouse and keyboard or a calibrated Bluetooth shin sensor.

## Open the project

1. Clone this repository and add its folder to Unity Hub.
2. Open it with the Unity Editor version listed in `ProjectSettings/ProjectVersion.txt`.
3. Allow Unity to import the assets and resolve packages.
4. Open `Assets/Scenes/MainMenu.unity` and enter Play mode.

## Prototype controls

- Move the mouse up/down to control the height marker.
- Press Space, or a configured hit key, when a note reaches the judgement line at the matching height. Settings supports up to two hit keys.
- For hold notes, keep a hit key held and maintain the height until the tail ends.
- Use Escape or the Pause button to pause gameplay.
- For sensor play, connect GAIT-SHANK and capture resting and raised references in Sensor Calibration. Notes are hit automatically at the matching height.

Therapy Mode keeps notes at the selected target height. Free Mode uses varied heights and difficulty levels. The prototype includes mode previews, a wellbeing check-in, local JSON settings and session history, rhythm calibration, hit feedback and space-themed visual effects. The classical collection includes licensed recordings of Für Elise, Canon in D and Bach’s Prelude in C, plus Mozart’s Rondo K.545 and Chopin’s Prelude in E minor, with individual Easy, Medium, Hard and Therapy charts for all five songs. Every session fits within 2:30.

Gameplay includes in-session audio and sensor settings, four wellbeing scenarios, and a sensor-removal reminder when leaving sensor play. Performance History supports selecting and deleting local records with confirmation. Music, beat/hit and UI volumes are separate; music uses a gentler volume curve across menus, previews and gameplay.

## Patient profiles

The game starts at the login screen. Sign in to browse patient profiles, then choose USE PROFILE. Guest play is available without Firebase configuration.

To enable sign-in after cloning, create `Assets/Resources` in Unity and use **Create > RhythmRehab > Firebase Client Config** inside it. Name the asset `FirebaseClientConfig` and set **Api Key** to the Firebase client API key provided by the project owner. Git ignores this asset and its `.meta` file. Unity includes the asset in the game build, so only enter the Firebase client API key. Firestore rules control access to patient records.

New sessions record the selected patient ID and name. History filters local sessions by patient and reads matching RhythmRehab sessions from the shared Firestore rehabilitationSessions collection. Cloud history is read-only here; the delete action removes local records. Older records without a patient ID stay with Guest. This feature reads Firebase and does not automatically upload sessions or modify patient details.

## Project folders

- `Assets/Scripts`: scripts grouped by feature; see [the folder guide](Assets/Scripts/README.md).
- `Assets/Scenes`: the game's scenes.
- `Assets/UI`, `Assets/Shaders`, `Assets/Data`: authored UI, shaders and song data.
- `Assets/Assets`: imported art, audio and packages.
- `Tools`: project authoring and verification utilities.
- `Screenshots`: development screenshots.
- `Packages`, `ProjectSettings`: Unity dependencies and project configuration.

Unity-generated caches and local editor settings are excluded from source control. Keep `.meta` files with the corresponding assets to preserve scene and Inspector references.
