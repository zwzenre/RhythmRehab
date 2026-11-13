using UnityEngine;

namespace RhythmRehab
{
    // A one-IMU movement reference, not an anatomical knee-angle measurement.
    public sealed class ShinHeightCalibration
    {
        private Vector3 rest, axis;
        public bool HasRest { get; private set; }
        public bool IsReady { get; private set; }
        public float RangeDegrees { get; private set; }
        public void Reset() { HasRest = IsReady = false; RangeDegrees = 0; }
        public void CaptureRest(Vector3 gravity)
        {
            rest = gravity.normalized;
            HasRest = rest.sqrMagnitude > .99f;
            IsReady = false; RangeDegrees = 0;
        }
        public bool CaptureRaised(Vector3 gravity, out string error)
        {
            error = null;
            if (!HasRest) { error = "Capture your resting position first."; return false; }
            gravity.Normalize();
            float range = Vector3.Angle(rest, gravity);
            if (gravity.sqrMagnitude < .99f || range < 10 || range > 150)
            { error = "The two positions need a clearer difference (10–150 degrees of sensor tilt). Try capturing again."; return false; }
            axis = Vector3.Cross(rest, gravity).normalized;
            RangeDegrees = range; IsReady = true; return true;
        }
        public float MovementDegrees(Vector3 gravity)
        {
            if (!IsReady) return 0;
            gravity.Normalize();
            return Mathf.Atan2(Vector3.Dot(axis, Vector3.Cross(rest, gravity)), Vector3.Dot(rest, gravity)) * Mathf.Rad2Deg;
        }
        public float Height(Vector3 gravity) => IsReady ? Mathf.Clamp01(MovementDegrees(gravity) / RangeDegrees) : 0;
    }
}
