using System;

namespace RhythmRehab
{
    public enum SessionMode { Therapy, Free }
    public enum HeightLevel { Low, Medium, High }
    public enum FreeDifficulty { Easy, Medium, Hard }

    public sealed class SessionProfile
    {
        public ChartNote[] chart;
        public float bpm;
        public float approachSeconds;
        public float heightTolerance;
        public double timingScale;
        public string label;
    }

    // Session choices survive scene changes. Scene UI and note slots remain authored objects.
    public static class SessionSelection
    {
        public static SongEntry Song { get; private set; }
        public static SessionMode Mode { get; private set; } = SessionMode.Therapy;
        public static HeightLevel Height { get; private set; } = HeightLevel.Low;
        public static FreeDifficulty Difficulty { get; private set; } = FreeDifficulty.Easy;
        public static bool Configured { get; private set; }

        public static void BeginBrowsing() { Configured = false; SessionWellbeing.Reset(); }
        public static void SelectSong(SongEntry song)
        {
            if (song == null || song.bpm <= 0) throw new ArgumentException("A valid song is required.");
            Song = song;
            Configured = false;
        }
        public static void SelectMode(SessionMode mode) { Mode = mode; Configured = false; }
        public static void SelectHeight(HeightLevel height) => Height = height;
        public static void SelectDifficulty(FreeDifficulty difficulty) => Difficulty = difficulty;
        public static void Confirm()
        {
            if (Song == null) throw new InvalidOperationException("Choose a song before starting.");
            Configured = true;
        }

        public static SessionProfile BuildProfile(SongEntry song, SessionMode mode, HeightLevel height, FreeDifficulty difficulty)
        {
            if (song == null || song.bpm < 60 || song.bpm > 180) throw new ArgumentException("Song tempo must be between 60 and 180 BPM.");
            int count = mode == SessionMode.Therapy ? 32 : difficulty == FreeDifficulty.Easy ? 16 : difficulty == FreeDifficulty.Medium ? 24 : 32;
            var notes = new ChartNote[count];
            // Normalized mouse/sensor height: 0 LOW, .5 MID, 1 HIGH.
            // Repeatable tempo demos, with gentler changes and a lower range on Easy.
            // Include exact halfway targets as well as the other intermediate heights.
            float[] easy = { 0, .12f, .25f, .36f, .5f, .34f, .18f, .42f };
            float[] medium = { 0, .25f, .44f, .68f, .88f, .56f, .30f, .12f, .38f, .75f, 1, .62f };
            float[] hard = { 0, .92f, .25f, .75f, .08f, .60f, .98f, .32f, .84f, .14f, .52f, 1 };
            float beat = 8;
            for (int i = 0; i < count; i++)
            {
                bool free = mode == SessionMode.Free;
                float target = !free ? (int)height * .5f : difficulty == FreeDifficulty.Easy ? easy[i % easy.Length] : difficulty == FreeDifficulty.Medium ? medium[i % medium.Length] : hard[i % hard.Length];
                float hold=i%6==3?2:0;
                notes[i] = new ChartNote { beat = beat, lane = free ? (int)Math.Round(target * 2) : (int)height, useCustomHeight = free, customHeight = target, holdBeats=hold };
                float interval=mode == SessionMode.Free && difficulty == FreeDifficulty.Easy ? 4 : mode == SessionMode.Free && difficulty == FreeDifficulty.Medium ? (i % 3 == 2 ? 4 : 2) : 2;
                // Leave recovery time after a hold before requesting another height.
                beat += (float)Math.Max(interval,hold+1);
            }
            return new SessionProfile
            {
                chart = notes,
                bpm = song.bpm,
                approachSeconds = mode == SessionMode.Therapy ? 3.5f : difficulty == FreeDifficulty.Hard ? 2.5f : difficulty == FreeDifficulty.Medium ? 3f : 3.5f,
                heightTolerance = mode == SessionMode.Therapy || difficulty == FreeDifficulty.Easy ? 0.18f : difficulty == FreeDifficulty.Medium ? 0.14f : 0.11f,
                timingScale = mode == SessionMode.Therapy ? 1.25 : difficulty == FreeDifficulty.Easy ? 1.3 : difficulty == FreeDifficulty.Medium ? 1 : 0.8,
                label = mode == SessionMode.Therapy ? "THERAPY · " + height.ToString().ToUpperInvariant() : "FREE · " + difficulty.ToString().ToUpperInvariant()
            };
        }
    }
}
