# RhythmRehab

A Unity rehabilitation rhythm-game prototype with a space theme. Players match incoming notes using mouse and keyboard or a calibrated Bluetooth shin sensor.

## Open the project

1. Clone this repository and add its folder to Unity Hub.
2. Open it with the Unity Editor version listed in `ProjectSettings/ProjectVersion.txt`.
3. To include the FC/AP celebration media, install the assets listed under [Award effects and audio](#award-effects-and-audio). Allow Unity to import the assets and resolve packages.
4. Open `Assets/Scenes/MainMenu.unity` and enter Play mode.

## Prototype controls

- Move the mouse up/down to control the height marker.
- Left-click, right-click, or press a configured hit key when a note reaches the judgement line at the matching height. Settings supports up to two keyboard bindings.
- For hold notes, keep a mouse button or hit key held and maintain the height until the tail ends.
- Use Escape or the Pause button to pause gameplay.
- For sensor play, connect GAIT-SHANK and capture resting and raised references in Sensor Calibration. Notes are hit automatically at the matching height.

Therapy Mode keeps notes at the selected target height. The Guided Reps toggle shows optional instructions when players select Start Rehab. Free Mode uses varied heights and difficulty levels. The prototype includes mode previews, a wellbeing check-in, local JSON settings and session history, rhythm calibration, hit feedback and space-themed visual effects. The classical collection includes licensed recordings of Für Elise, Canon in D and Bach’s Prelude in C, plus Mozart’s Rondo K.545 and Chopin’s Prelude in E minor, with individual Easy, Medium, Hard and Therapy charts for all five songs. Every session fits within 2:30.

Gameplay includes in-session audio and sensor settings, four wellbeing scenarios, and a sensor-removal reminder when leaving sensor play. Performance History supports selecting and deleting local records with confirmation. Music, beat/hit and UI volumes are separate; music uses a gentler volume curve across menus, previews and gameplay.

## Patient profiles

The game starts at the login screen. Sign in to browse patient profiles, then choose USE PROFILE. Guest play is available without Firebase configuration.

To enable sign-in after cloning, create `Assets/Resources` in Unity and use **Create > RhythmRehab > Firebase Client Config** inside it. Name the asset `FirebaseClientConfig` and set **Api Key** to the Firebase client API key provided by the project owner. Git ignores this asset and its `.meta` file. Unity includes the asset in the game build, so only enter the Firebase client API key. Firestore rules control access to patient records.

New sessions record the selected patient ID and name. History filters local sessions by patient and reads matching RhythmRehab sessions from the shared Firestore `rehabilitationSessions` collection. The delete action removes local records. Older records without a patient ID stay with Guest.

For Therapy with an active `GAME003` leg-raise plan, Unity uses the plan's minimum, midpoint and maximum shin tilt as the Low, Medium and High targets. Players can practice the plan using mouse controls. Only sessions played entirely with a calibrated shin sensor are eligible for upload. History → Review Queue → Upload Selected uploads selected local sensor sessions. The shared session fields carry Overall percentage as `finalScore` and successful raises divided by the prescribed total as `completionRate`. Song details stay in local history. Upload uses the session ID as the Firestore document ID. Before each upload, Unity sets `scheduledSessionIndex` to one plus the count of Firestore sessions marked `Completed` for that patient and plan. Early-ended sessions leave the same slot open. Unity does not manage set breaks.

## Free Mode leaderboard

Open Leaderboard from Song Selection to browse the top ten for each song, with separate Easy, Medium and Hard rankings for mouse and keyboard or sensor. Completed Free Mode runs using one control type can be published from Session Summary. Guests and signed-in players enter a public display name. Each ranking keeps the highest Overall percentage for that name, ignoring case and extra spaces.

The game uses the existing Firestore project and writes only the chosen display name and score to `rhythmRehabLeaderboards/{ranking}/entries/{nameKey}`. The project’s Firestore rules determine whether guests can read and write there.

## Award effects and audio

The Windows release includes the award effects and sounds. For an Editor checkout, import [Cartoon FX Remaster Free](https://assetstore.unity.com/packages/vfx/particles/cartoon-fx-remaster-free-109565) with its original Unity metadata. RhythmRehab uses the pack's flare and electric spark textures.

Download **Bonus Earned in Video Game** from [Mixkit Game Sounds](https://mixkit.co/free-sound-effects/game/) and **Fairy Sparkle Whoosh** from [Mixkit Sparkle Sounds](https://mixkit.co/free-sound-effects/sparkle/). Place the WAV files in `Assets/Assets` as `mixkit-bonus-earned-in-video-game-2058.wav` and `mixkit-fairy-sparkle-whoosh-869.wav` before the first Unity import. Keep the corresponding checked-in `.meta` files so the scene's audio references resolve.

The white arrow cursor is adapted from [Ergonomic Cursors by Sky](https://skyops.itch.io/ergonomic-cursors), licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The arrow artwork was scaled down within its original texture dimensions.

## Project folders

- `Assets/Scripts`: scripts grouped by feature; see [the folder guide](Assets/Scripts/README.md).
- `Assets/Scenes`: the game's scenes.
- `Assets/UI`, `Assets/Shaders`, `Assets/Data`: authored UI, shaders and song data.
- `Assets/Assets`: imported art, audio and packages.
- `Tools`: project authoring and verification utilities.
- `Screenshots`: development screenshots.
- `Packages`, `ProjectSettings`: Unity dependencies and project configuration.

Unity-generated caches and local editor settings are excluded from source control. Keep `.meta` files with the corresponding assets to preserve scene and Inspector references.
