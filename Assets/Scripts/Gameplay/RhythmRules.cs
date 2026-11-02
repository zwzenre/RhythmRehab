using System;

namespace RhythmRehab
{
    public enum HitJudgement { Perfect, Great, Good, Miss }

    public static class RhythmRules
    {
        public const double PerfectWindow = 0.08;
        public const double GreatWindow = 0.14;
        public const double GoodWindow = 0.22;

        public static HitJudgement Judge(double timingError, float heightError, float tolerance, double timingScale = 1)
        {
            if (heightError > tolerance || Math.Abs(timingError) > GoodWindow * timingScale) return HitJudgement.Miss;
            if (Math.Abs(timingError) <= PerfectWindow * timingScale) return HitJudgement.Perfect;
            return Math.Abs(timingError) <= GreatWindow * timingScale ? HitJudgement.Great : HitJudgement.Good;
        }

        public static float Weight(HitJudgement judgement)
        {
            switch (judgement)
            {
                case HitJudgement.Perfect: return 1f;
                case HitJudgement.Great: return 0.8f;
                case HitJudgement.Good: return 0.5f;
                default: return 0f;
            }
        }

        public static int Points(HitJudgement judgement)
        {
            switch (judgement)
            {
                case HitJudgement.Perfect: return 1000;
                case HitJudgement.Great: return 700;
                case HitJudgement.Good: return 400;
                default: return 0;
            }
        }
    }
}
