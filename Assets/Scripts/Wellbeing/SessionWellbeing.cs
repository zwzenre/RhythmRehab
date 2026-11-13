using System;
using UnityEngine;

namespace RhythmRehab
{
    [Serializable]
    public sealed class WellbeingResponse
    {
        public bool skipped;
        public int pain = -1;
        public int stress = -1;
        public bool highPain;
        public bool highStress;
        public bool reducedPerformanceFeedback;
        public string recordedUtc;

        public WellbeingResponse Copy() => new WellbeingResponse
        {
            skipped = skipped, pain = pain, stress = stress, highPain = highPain,
            highStress = highStress, reducedPerformanceFeedback = reducedPerformanceFeedback,
            recordedUtc = recordedUtc
        };
    }

    public static class SessionWellbeing
    {
        private static WellbeingResponse current;
        public static WellbeingResponse Snapshot => current?.Copy();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() => current = null;
        public static void Record(int pain, int stress, int highPainThreshold, int highStressThreshold)
        {
            pain = Mathf.Clamp(pain, 0, 10); stress = Mathf.Clamp(stress, 0, 10);
            bool highPain = pain >= highPainThreshold, highStress = stress >= highStressThreshold;
            current = new WellbeingResponse
            {
                pain = pain, stress = stress, highPain = highPain, highStress = highStress,
                reducedPerformanceFeedback = highPain || highStress, recordedUtc = DateTime.UtcNow.ToString("O")
            };
        }
        public static void Skip() => current = new WellbeingResponse { skipped = true, recordedUtc = DateTime.UtcNow.ToString("O") };
        public static string ScenarioLabel(WellbeingResponse response)
        {
            if (response == null || response.skipped) return "Not assessed";
            if (response.highPain && response.highStress) return "High Pain & Stress";
            if (response.highPain) return "High Pain";
            if (response.highStress) return "High Stress";
            return "Normal";
        }
        public static string GameplayMessage(WellbeingResponse response)
        {
            if (response == null || response.skipped) return "Follow the rhythm and raise your leg to the target height.";
            if (response.highPain && response.highStress) return "Move gently and take your time. Pause or stop whenever you need.";
            if (response.highPain) return "Move gently within a comfortable range. Stop if you feel pain or discomfort.";
            if (response.highStress) return "Take your time and focus on the rhythm. There is no need to worry about your score.";
            return "Follow the rhythm and raise your leg to the target height.";
        }
    }
}
