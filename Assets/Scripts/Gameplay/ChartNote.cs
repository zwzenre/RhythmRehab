using System;
using UnityEngine;

namespace RhythmRehab
{
    [Serializable]
    public struct ChartNote
    {
        public const float LowTargetHeight = .24f;
        public const float MidTargetHeight = (LowTargetHeight + 1f) * .5f;
        public float beat;
        [Range(0, 2)] public int lane;
        [Tooltip("Use a height between the guide lines instead of a fixed lane.")]
        public bool useCustomHeight;
        [Range(0, 1)] public float customHeight;
        [Min(0), Tooltip("Duration in beats. Zero is a tap note.")]
        public float holdBeats;
        public bool IsHold => holdBeats > .01f;
        public float EndBeat => beat + Mathf.Max(0, holdBeats);
        public float TargetHeight
        {
            get
            {
                float authoredHeight = useCustomHeight ? Mathf.Clamp01(customHeight) : Mathf.Clamp(lane, 0, 2) * .5f;
                return Mathf.Lerp(LowTargetHeight, 1f, authoredHeight);
            }
        }
    }
}
