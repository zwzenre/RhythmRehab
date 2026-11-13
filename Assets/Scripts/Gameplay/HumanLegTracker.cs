using UnityEngine;

namespace RhythmRehab
{
    // The model, camera, chair and UI view are all authored in the scene.
    public sealed class HumanLegTracker : MonoBehaviour
    {
        [SerializeField] private PrototypeGameplay gameplay;
        [SerializeField] private Transform model;
        [SerializeField] private Transform lowerLeg;
        [SerializeField, Min(0f)] private float maximumVisualAngle = 30f;
        [Header("Sensor calibration preview")]
        [SerializeField] private bool sensorCalibrationPreview;
        [SerializeField] private TMPro.TMP_Text calibrationCaption;
        private Quaternion restingRotation;
        private Transform thigh;
        private Vector3 bendAxis;
        public float DisplayedHeight { get; private set; }

        private void Awake()
        {
            thigh = lowerLeg.parent;
            restingRotation = thigh.localRotation;
            bendAxis = thigh.parent.InverseTransformDirection(model.right).normalized;
        }

        private void LateUpdate()
        {
            if (sensorCalibrationPreview)
            {
                var sensor = UsbSensorConnection.Instance;
                bool ready = sensor && sensor.Calibration.IsReady;
                float target = ready ? sensor.Height : sensor && sensor.Calibration.HasRest ? 1 : 0;
                ApplyHeight(Mathf.MoveTowards(DisplayedHeight, target, Time.unscaledDeltaTime * .8f));
                if (calibrationCaption) calibrationCaption.text = ready ? sensor.HasFreshReading ? "LIVE SENSOR MOVEMENT" : "SENSOR DATA PAUSED" :
                    sensor && sensor.Calibration.HasRest ? "RAISED POSITION EXAMPLE" : "RESTING POSITION";
                return;
            }
            if (gameplay && !gameplay.IsPaused) ApplyHeight(gameplay.CurrentHeight);
        }

        public void ApplyHeight(float height)
        {
            DisplayedHeight = Mathf.Clamp01(height);
            thigh.localRotation = Quaternion.AngleAxis(-DisplayedHeight * maximumVisualAngle, bendAxis) * restingRotation;
        }
    }
}
