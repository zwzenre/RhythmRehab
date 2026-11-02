# RhythmRehab

A Unity rehabilitation rhythm-game prototype with a space theme. Players match incoming notes by controlling a height marker and pressing Space. The current version uses mouse input while sensor integration is under development.

## Open the project

1. Clone this repository and add its folder to Unity Hub.
2. Open it with the Unity Editor version listed in `ProjectSettings/ProjectVersion.txt`.
3. Allow Unity to import the assets and resolve packages.
4. Open `Assets/Scenes/MainMenu.unity` and enter Play mode.

## Prototype controls

- Move the mouse up/down to control the height marker.
- Press Space when a note reaches the judgement line at the matching height.
- For hold notes, keep Space held and maintain the height until the tail ends.
- Use Escape or the Pause button to pause gameplay.

Therapy Mode keeps notes at the selected target height. Free Mode uses varied heights and difficulty levels. The prototype includes mode previews, a wellbeing check-in, local JSON settings and session history, rhythm calibration, hit feedback and space-themed visual effects. Song entries currently use generated tempo beats. Sensor connection and calibration are frameworks awaiting device integration.

## Project folders

- `Assets/Scripts`: scripts grouped by feature; see [the folder guide](Assets/Scripts/README.md).
- `Assets/Scenes`: the game's scenes.
- `Assets/UI`, `Assets/Shaders`, `Assets/Data`: authored UI, shaders and song data.
- `Assets/Assets`: imported art, audio and packages.
- `Tools`: project authoring and verification utilities.
- `Screenshots`: development screenshots.
- `Packages`, `ProjectSettings`: Unity dependencies and project configuration.

Unity-generated caches and local editor settings are excluded from source control. Keep `.meta` files with the corresponding assets to preserve scene and Inspector references.
