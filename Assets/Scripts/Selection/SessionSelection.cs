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
        public float durationSeconds;
    }

    // Session choices survive scene changes. Scene UI and note slots remain authored objects.
    public static class SessionSelection
    {
        public static SongEntry Song { get; private set; }
        public static SessionMode Mode { get; private set; } = SessionMode.Therapy;
        public static HeightLevel Height { get; private set; } = HeightLevel.Low;
        public static FreeDifficulty Difficulty { get; private set; } = FreeDifficulty.Easy;
        public static bool Configured { get; private set; }

        public static RehabilitationPlan Plan { get; private set; }
        public static bool ShowGuidedReps { get; private set; }
        public static void BeginBrowsing() { Configured = false; AssignPlan(null); SetGuidedReps(false); SessionWellbeing.Reset(); }
        public static void SelectSong(SongEntry song)
        {
            if (song == null || song.bpm <= 0) throw new ArgumentException("A valid song is required.");
            Song = song;
            Configured = false;
            AssignPlan(null);
            SetGuidedReps(false);
        }
        public static void SelectMode(SessionMode mode) { Mode = mode; Configured = false; AssignPlan(null); SetGuidedReps(false); }
        public static void AssignPlan(RehabilitationPlan plan) => Plan = plan;
        public static void SetGuidedReps(bool show) => ShowGuidedReps = show && Mode == SessionMode.Therapy;
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
            int count = mode == SessionMode.Therapy ? 32 : difficulty == FreeDifficulty.Easy ? 24 : difficulty == FreeDifficulty.Medium ? 32 : 44;
            var notes = new ChartNote[count];
            // Normalized mouse/sensor height: 0 LOW, .5 MID, 1 HIGH.
            // Free tiers now start with the former Medium and Hard patterns.
            // Include exact halfway targets as well as the other intermediate heights.
            float[] easy = { 0, .25f, .44f, .68f, .88f, .56f, .30f, .12f, .38f, .75f, 1, .62f };
            float[] medium = { 0, .92f, .25f, .75f, .08f, .60f, .98f, .32f, .84f, .14f, .52f, 1 };
            float[] hard = { .12f, .36f, .62f, .88f, .68f, .44f, .20f, .48f, .75f, 1, .72f, .46f };
            float beat = 8;
            for (int i = 0; i < count; i++)
            {
                bool free = mode == SessionMode.Free;
                float target = !free ? (int)height * .5f : difficulty == FreeDifficulty.Easy ? easy[i % easy.Length] : difficulty == FreeDifficulty.Medium ? medium[i % medium.Length] : hard[i % hard.Length];
                float hold=i%6==3?2:0;
                notes[i] = new ChartNote { beat = beat, lane = free ? (int)Math.Round(target * 2) : (int)height, useCustomHeight = free, customHeight = target, holdBeats=hold };
                float interval=mode == SessionMode.Therapy ? 2 : difficulty == FreeDifficulty.Easy ? (i % 3 == 2 ? 4 : 2) : difficulty == FreeDifficulty.Medium ? 2 : 1;
                // Leave recovery time after a hold before requesting another height.
                beat += (float)Math.Max(interval,hold+1);
            }
            var profile = new SessionProfile
            {
                chart = notes,
                bpm = song.bpm,
                approachSeconds = mode == SessionMode.Therapy ? 3.5f : difficulty == FreeDifficulty.Hard ? 2.3f : difficulty == FreeDifficulty.Medium ? 2.5f : 3f,
                heightTolerance = mode == SessionMode.Therapy ? 0.18f : difficulty == FreeDifficulty.Easy ? 0.14f : 0.11f,
                timingScale = mode == SessionMode.Therapy ? 1.25 : difficulty == FreeDifficulty.Easy ? 1 : 0.8,
                label = mode == SessionMode.Therapy ? "THERAPY · " + height.ToString().ToUpperInvariant() : "FREE · " + difficulty.ToString().ToUpperInvariant()
            };
            var authored = mode == SessionMode.Therapy ? song.therapyChart : difficulty == FreeDifficulty.Easy ? song.easyChart : difficulty == FreeDifficulty.Medium ? song.mediumChart : song.hardChart;
            if (song.audio && authored != null && authored.notes != null && authored.notes.Length > 0)
            {
                profile.chart = (ChartNote[])authored.notes.Clone();
                profile.durationSeconds = System.Math.Max(authored.durationSeconds, song.audioLeadInSeconds + song.audio.length);
                if (mode == SessionMode.Therapy)
                    for (int i = 0; i < profile.chart.Length; i++)
                    { profile.chart[i].lane = (int)height; profile.chart[i].useCustomHeight = false; }
            }
            return profile;
        }
    }
}
