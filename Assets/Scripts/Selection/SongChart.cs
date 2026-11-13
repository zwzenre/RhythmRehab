using UnityEngine;

namespace RhythmRehab
{
    [CreateAssetMenu(menuName = "RhythmRehab/Song Chart")]
    public sealed class SongChart : ScriptableObject
    {
        [TextArea(2, 5)] public string description;
        public ChartNote[] notes;
        [Min(0)] public float durationSeconds;
    }
}
