# Gameplay script guide

The Gameplay scene uses separate scripts with one clear responsibility each.
All existing note slots, UI objects, audio sources and effect objects remain authored in the scene.

| Script in Assets/Scripts | What to change here |
| --- | --- |
| Gameplay/PrototypeGameplay.cs | Session setup, input coordination, pause buttons, completion and navigation |
| Gameplay/GameplayChartRunner.cs | Choosing the note to hit, timing/height checks, holds, misses, score and combo |
| Gameplay/GameplayPlayfield.cs | Mouse height mapping, note movement, note colours, marker and hold visuals |
| Gameplay/GameplayHud.cs | Score/accuracy text, judgement feedback, countdown, progress and pause panel |
| Audio/GameplayAudio.cs | Beat playback, hit sound and audio pause/cleanup |
| Gameplay/GameplaySessionClock.cs | Elapsed song time and freezing/resuming time |
| Gameplay/ChartNote.cs | The data describing one note, including its height and hold duration |

Select **Controllers** in the Gameplay hierarchy. Prototype Gameplay references the Gameplay Playfield, Gameplay Hud and Gameplay Audio components on that same object. Their editable references and layout values are in their respective Inspector components.

Existing specialised scripts still handle their own features: HoldNoteState handles hold rules; HoldNoteView draws the authored hold graphics; NoteHitEffects handles successful-hit particles and ripples; GameplayVisualLevels handles space themes; HumanLegTracker handles the body visual.

Flow: input -> PrototypeGameplay -> GameplayChartRunner -> result event -> HUD, audio and effects. Each frame the coordinator also asks the playfield to draw the current note positions.

GameplayChartRunner and GameplaySessionClock are plain C# classes created by the coordinator; they do not need GameObjects or Inspector components.

Assets/Scripts/Editor/GameplayEditorAccess.cs supports older editor-only authoring and verification tools after fields moved to the new components. It is excluded from game builds. MigrateGameplayResponsibilities and BindGameplayResponsibilities are one-time migration tools, not normal gameplay setup steps.
