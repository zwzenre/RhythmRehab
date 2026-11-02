using UnityEngine;

namespace RhythmRehab
{
    public static class RhythmPreferences
    {
        public const string OffsetKey = "RhythmRehab.JudgementOffsetMs";
        public const string SpeedKey = "RhythmRehab.NoteSpeed";
        public static float OffsetMs => LocalSettingsStore.Default.Load(out _).judgementOffsetMs;
        public static float Speed => LocalSettingsStore.Default.Load(out _).noteSpeed;
        public static float TravelSeconds(float original, float speed) => original / Mathf.Clamp(speed, .5f, 2);
        // A positive offset accepts a later press. The musical beat and visual line do not move.
        public static double TimingError(double time, double beatTime, float offsetSeconds) => time - beatTime - offsetSeconds;
        public static void Save(float offsetMs, float speed)
        {
            if (!TrySave(offsetMs, speed, out var error)) Debug.LogWarning("Rhythm settings could not be saved: " + error);
        }
        public static bool TrySave(float offsetMs, float speed, out string error) =>
            LocalSettingsStore.Default.TrySaveRhythm(offsetMs, speed, out error);
    }
}
