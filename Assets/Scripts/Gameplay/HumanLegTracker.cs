using UnityEngine;

namespace RhythmRehab
{
    // The model, camera, chair and UI view are all authored in the scene.
    public sealed class HumanLegTracker : MonoBehaviour
    {
        [SerializeField] private PrototypeGameplay gameplay;
        [SerializeField] private Transform model;
        [SerializeField] private Transform lowerLeg;
        [SerializeField, Min(0f)] private float maximumVisualAngle = 90f;
        private Quaternion restingRotation;
        private Vector3 bendAxis;
        public float DisplayedHeight { get; private set; }

        private void Awake()
        {
            restingRotation = lowerLeg.localRotation;
            bendAxis = lowerLeg.parent.InverseTransformDirection(model.right).normalized;
        }

        private void LateUpdate()
        {
            if (gameplay && !gameplay.IsPaused) ApplyHeight(gameplay.CurrentHeight);
        }

        public void ApplyHeight(float height)
        {
            DisplayedHeight = Mathf.Clamp01(height);
            lowerLeg.localRotation = Quaternion.AngleAxis(-DisplayedHeight * maximumVisualAngle, bendAxis) * restingRotation;
        }
    }
}
