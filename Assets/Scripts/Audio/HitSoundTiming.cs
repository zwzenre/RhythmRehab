using UnityEngine;

namespace RhythmRehab
{
    public static class HitSoundTiming
    {
        // Input feedback cannot precede the input. Negative values skip the clip's lead-in;
        // positive values prepend silence. Neither changes note judgement or musical timing.
        public static AudioClip Adjust(AudioClip original, float offsetMs)
        {
            if (!original || Mathf.Abs(offsetMs) < .5f) return original;
            int frames = Mathf.RoundToInt(Mathf.Clamp(offsetMs, -250, 250) * original.frequency / 1000f);
            int skip = Mathf.Clamp(-frames, 0, original.samples - 1);
            int padding = Mathf.Max(0, frames);
            var input = new float[original.samples * original.channels];
            if (!original.GetData(input, 0))
            { Debug.LogWarning("Hit sound timing needs a readable, decompressed audio clip."); return original; }
            int remaining = original.samples - skip;
            var samples = new float[(remaining + padding) * original.channels];
            System.Array.Copy(input, skip * original.channels, samples, padding * original.channels, remaining * original.channels);
            var adjusted = AudioClip.Create(original.name + " (hit timing)", remaining + padding, original.channels, original.frequency, false);
            adjusted.SetData(samples, 0);
            return adjusted;
        }
    }
}
