using System;

namespace RhythmRehab
{
    [Serializable]
    public sealed class SessionSummary
    {
        public int totalNotes;
        public int perfect;
        public int great;
        public int good;
        public int miss;
        public int score;
        public int bestCombo;
        public float duration;
        public string songTitle;
        public string modeLabel;
        public float bpm;
        public string sessionId = Guid.NewGuid().ToString("N");
        public string startedUtc = DateTime.UtcNow.ToString("O");
        public string endedUtc;
        public bool endedEarly;
        public int heightAttempts;
        public int correctHeightAttempts;
        public int holdNotes;
        public int completedHolds;
        public int brokenHolds;
        public WellbeingResponse wellbeing;
        [NonSerialized] public string historySaveError;
        public int Judged => perfect + great + good + miss;
        public float Accuracy => Judged == 0 ? 0 : (perfect + great * 0.8f + good * 0.5f) / Judged;
        public float Completion => totalNotes == 0 ? 0 : (float)Judged / totalNotes;
        public static SessionSummary Last { get; set; }

        public void Record(HitJudgement judgement)
        {
            switch (judgement)
            {
                case HitJudgement.Perfect: perfect++; break;
                case HitJudgement.Great: great++; break;
                case HitJudgement.Good: good++; break;
                default: miss++; break;
            }
            score += RhythmRules.Points(judgement);
        }
    }
}
