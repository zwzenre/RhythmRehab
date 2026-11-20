using UnityEngine;

namespace RhythmRehab
{
    public sealed class GameplayAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource beatSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource hitSource;
        private AudioClip beatClip, adjustedHitClip, originalHitClip;
        private double scheduledDsp, remainingLead;
        private bool audioPaused, waitingToStart;
        private AudioSource playbackSource;
        public AudioSource MusicSource => musicSource;
        public AudioSource BeatSource => beatSource;
        public AudioSource HitSource => hitSource;
        public void Initialize(LocalSettings settings, float bpm, float duration, double startDsp, AudioClip song = null, float leadIn = 0, bool loop = false)
        {
            ApplyVolumes(settings.musicVolume, settings.beatVolume);
            musicSource.Stop(); beatSource.Stop();
            originalHitClip = hitSource.clip; adjustedHitClip = HitSoundTiming.Adjust(originalHitClip, settings.hitSoundOffsetMs);
            playbackSource = song ? musicSource : beatSource;
            playbackSource.loop = loop;
            if (song) musicSource.clip = song;
            else { beatClip = MakeMetronome(bpm, duration); beatSource.clip = beatClip; }
            scheduledDsp = startDsp + (song ? leadIn : 0); audioPaused = waitingToStart = false;
            playbackSource.PlayScheduled(scheduledDsp);
        }
        public void PlayHit() { if (adjustedHitClip) hitSource.PlayOneShot(adjustedHitClip); }
        public void ApplyVolumes(float music, float beat)
        {
            musicSource.volume = AudioVolume.MusicGain(music);
            beatSource.volume = hitSource.volume = beat;
        }
        public void SetPaused(bool paused)
        {
            if (paused == audioPaused) return;
            audioPaused = paused;
            if (paused)
            {
                waitingToStart = AudioSettings.dspTime < scheduledDsp;
                if (waitingToStart) { remainingLead = scheduledDsp - AudioSettings.dspTime; playbackSource.Stop(); }
                else playbackSource.Pause();
                hitSource.Pause();
            }
            else
            {
                if (waitingToStart) { scheduledDsp = AudioSettings.dspTime + remainingLead; playbackSource.PlayScheduled(scheduledDsp); waitingToStart = false; }
                else playbackSource.UnPause();
                hitSource.UnPause();
            }
        }
        public void Stop() { musicSource.Stop(); beatSource.Stop(); hitSource.Stop(); audioPaused = waitingToStart = false; }
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
