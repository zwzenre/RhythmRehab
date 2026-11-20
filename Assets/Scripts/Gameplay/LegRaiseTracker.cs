using UnityEngine;

namespace RhythmRehab
{
    internal sealed class LegRaiseTracker
    {
        private readonly float minimum, maximum, resetHeight, successfulHeight;
        private float peak;
        private bool isArmed;

        public LegRaiseTracker(RehabilitationPlan plan, float target)
        {
            minimum = plan.minimumDegrees;
            maximum = plan.maximumDegrees;
            resetHeight = minimum * .5f;
            successfulHeight = Mathf.Max(minimum, target - 3f);
        }

        public bool Record(float degrees, SessionSummary summary)
        {
            if (degrees > resetHeight)
            {
                if (isArmed) peak = Mathf.Max(peak, degrees);
                return false;
            }

            bool attempted = isArmed && peak >= minimum;
            bool successful = attempted && peak >= successfulHeight && peak <= maximum;
            if (attempted) summary.legRaises++;
            if (successful) summary.successfulLegRaises++;
            isArmed = true;
            peak = degrees;
            return successful;
        }

        public void Reset() => isArmed = false;
    }
}
