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
        public bool sensorHeightControl;
        public string patientId;
        public string patientName;
        [NonSerialized] public bool isCloudRecord;
        [NonSerialized] public float cloudAccuracy = -1;
        [NonSerialized] public float cloudCompletion = -1;
        public WellbeingResponse wellbeing;
        [NonSerialized] public string historySaveError;
        public int Judged => perfect + great + good + miss;
        public bool HasAccuracy => Judged > 0 || (isCloudRecord && cloudAccuracy >= 0);
        public float Accuracy => isCloudRecord && cloudAccuracy >= 0 ? UnityEngine.Mathf.Clamp01(cloudAccuracy) : Judged == 0 ? 0 : (perfect + great * 0.8f + good * 0.5f) / Judged;
        public float Completion => isCloudRecord && cloudCompletion >= 0 ? UnityEngine.Mathf.Clamp01(cloudCompletion) : totalNotes == 0 ? 0 : (float)Judged / totalNotes;
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
            double earned = perfect * (double)RhythmRules.Points(HitJudgement.Perfect)
                + great * (double)RhythmRules.Points(HitJudgement.Great)
                + good * (double)RhythmRules.Points(HitJudgement.Good);
            // Round the accumulated score once so charts with fractional note values
            // still finish at exactly 10,000 for an all-Perfect performance.
            score = totalNotes <= 0 ? 0 : (int)Math.Round(
                RhythmRules.MaximumScore * earned / (totalNotes * (double)RhythmRules.Points(HitJudgement.Perfect)),
                MidpointRounding.AwayFromZero);
        }
    }
}
