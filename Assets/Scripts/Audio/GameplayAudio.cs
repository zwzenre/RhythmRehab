using UnityEngine;

namespace RhythmRehab
{
    public sealed class GameplayAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource beatSource;
        [SerializeField] private AudioSource hitSource;
        private AudioClip beatClip, adjustedHitClip, originalHitClip;
        public void Initialize(LocalSettings settings, float bpm, float duration, double startDsp)
        {
            beatSource.volume = settings.beatVolume; hitSource.volume = settings.sfxVolume;
            originalHitClip = hitSource.clip; adjustedHitClip = HitSoundTiming.Adjust(originalHitClip, settings.hitSoundOffsetMs);
            beatClip = MakeMetronome(bpm, duration); beatSource.clip = beatClip; beatSource.PlayScheduled(startDsp);
        }
        public void PlayHit() { if (adjustedHitClip) hitSource.PlayOneShot(adjustedHitClip); }
        public void SetPaused(bool paused) { if (paused) { beatSource.Pause(); hitSource.Pause(); } else { beatSource.UnPause(); hitSource.UnPause(); } }
        public void Stop() { beatSource.Stop(); hitSource.Stop(); }
        private static AudioClip MakeMetronome(float bpm, float duration)
        {
            const int rate = 22050; var samples = new float[Mathf.CeilToInt(duration * rate)];
            int step = Mathf.RoundToInt(60f / Mathf.Max(1, bpm) * rate);
            for (int beat = 0, offset = 0; offset < samples.Length; beat++, offset += step)
                for (int n = 0; n < 1100 && offset + n < samples.Length; n++)
                    samples[offset + n] = Mathf.Sin(2 * Mathf.PI * (beat % 4 == 0 ? 1400 : 950) * n / rate) * Mathf.Exp(-n / 180f) * .5f;
            var clip = AudioClip.Create($"{bpm:0} BPM prototype beat", samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private void OnDestroy() { if (beatClip) Destroy(beatClip); if (adjustedHitClip && adjustedHitClip != originalHitClip) Destroy(adjustedHitClip); }
    }
}
