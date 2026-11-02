# Local saves

The game saves to Unity's `Application.persistentDataPath`, outside the Assets folder.
For the current Windows project this is:
`C:/Users/USER/AppData/LocalLow/DefaultCompany/RhythmRehab/`

- `session-history.json`: completed sessions and sessions explicitly ended with Finish. Includes ID, UTC timestamps, song, mode, BPM, score, judgements, combo, duration and height-match attempts. Profile reads this file. Restarting or returning to the menu abandons the current attempt; calibration practice is excluded.

Session records also include `wellbeing` when the pre-session check-in was completed: pain and stress (0–10), high flags, reduced-performance-feedback flag and UTC assessment time. Both high thresholds are 8 in the assessment controller Inspector. Skip stores `skipped=true` and ratings `-1`, distinct from reporting zero. Older records without `wellbeing` remain valid. High ratings hide score, accuracy, combo, progress and remaining time during gameplay; scoring still runs and the result/history is saved normally. Main Menu Start clears the previous response and opens the assessment before Song Selection.
- `settings.json`: beat/music volume, SFX volume, hit-sound timing (-250 to +250 ms), judgement offset in milliseconds and note-speed multiplier. Volume sliders save immediately. Judgement offset, note speed and hit-sound timing are all on Rhythm Calibration and save together when SAVE is pressed. Test Sound and successful practice hits use the current draft sound timing. Values load at scene startup. Negative sound timing skips the clip's lead-in, positive timing adds silence; it does not change note judgement. Existing settings files default this added field to zero. Sensor values are not saved yet because hardware integration is still a framework.

Existing PlayerPrefs settings migrate on the first settings-file creation. Subsequent changes use JSON. Both stores use a temporary file and atomic replacement, and preserve unreadable/unsupported files rather than overwriting them. Settings load defaults if a file cannot be read; audio errors are logged and calibration Save displays failures.

The folder persists across game restarts. Changing Company Name or Product Name in Player Settings changes Unity's save location. These are local files: they do not synchronize or provide cloud backup.

Music/beat volume also controls menu background music; SFX volume controls UI clicks and successful note hits. Each non-gameplay scene's saved ScreenAudio object picks and loops one of the three music tracks on entry. Gameplay contains click audio only. Rhythm practice pauses background music and resumes it when practice stops. Random choices may repeat between consecutive screens.

Verification tools (run against the connected Unity Editor):
- `node Tools/check-local-saving.cjs`: isolated disk round trips, migration, validation and existing session/scoring checks.
- `node Tools/test-local-saving-live.cjs`: actual sliders, calibration Save, Play mode restart and UI reload; preserves existing user settings and does not create test session history.
