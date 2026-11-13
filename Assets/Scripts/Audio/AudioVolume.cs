using UnityEngine;

namespace RhythmRehab
{
    public static class AudioVolume
    {
        // A linear amplitude slider sounds loud around its midpoint.
        // This curve gives the quieter half of the slider more useful control.
        public static float MusicGain(float value)
        {
            float level = Mathf.Clamp01(value);
            return level * level;
        }
    }
}
