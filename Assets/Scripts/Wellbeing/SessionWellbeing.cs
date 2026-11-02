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
        public static string GameplayMessage(WellbeingResponse response)
        {
            if (response == null || response.skipped) return null;
            if (response.highPain && response.highStress) return "Move gently and stop if you feel discomfort. Take your time and follow the rhythm comfortably.";
            if (response.highPain) return "Please perform the exercise gently and stop if you experience discomfort.";
            if (response.highStress) return "Take your time and focus on following the rhythm comfortably. Your score will not be displayed.";
            return null;
        }
    }
}
