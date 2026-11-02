using System;
using UnityEngine;

namespace RhythmRehab
{
    [Serializable]
    public struct ChartNote
    {
        public float beat;
        [Range(0, 2)] public int lane;
        [Tooltip("Use a height between the guide lines instead of a fixed lane.")]
        public bool useCustomHeight;
        [Range(0, 1)] public float customHeight;
        [Min(0), Tooltip("Duration in beats. Zero is a tap note.")]
        public float holdBeats;
        public bool IsHold => holdBeats > .01f;
        public float EndBeat => beat + Mathf.Max(0, holdBeats);
        public float TargetHeight => useCustomHeight ? Mathf.Clamp01(customHeight) : Mathf.Clamp(lane, 0, 2) * .5f;
    }
}
