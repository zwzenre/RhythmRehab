using UnityEngine;

namespace RhythmRehab
{
    // Sources and clips are saved in each scene. Gameplay has click audio only.
    public sealed class UIScreenAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource backgroundSource;
        [SerializeField] private AudioSource clickSource;
        [SerializeField] private AudioClip[] backgroundTracks;
        public int ClickCount { get; private set; }
        public AudioClip CurrentTrack => backgroundSource ? backgroundSource.clip : null;
        public bool BackgroundPlaying => backgroundSource && backgroundSource.isPlaying;

        private void Start()
        {
            var settings = LocalSettingsStore.Default.Load(out _);
            ApplyVolumes(settings.beatVolume, settings.sfxVolume);
            if (backgroundSource && backgroundTracks != null && backgroundTracks.Length > 0)
            {
                backgroundSource.clip = backgroundTracks[Random.Range(0, backgroundTracks.Length)];
                backgroundSource.Play();
            }
        }

        public void ApplyVolumes(float music, float sfx)
        {
            if (backgroundSource) backgroundSource.volume = Mathf.Clamp01(music);
            if (clickSource) clickSource.volume = Mathf.Clamp01(sfx);
        }

        public void PlayClick()
        {
            if (!clickSource || !clickSource.clip) return;
            clickSource.PlayOneShot(clickSource.clip);
            ClickCount++;
        }

        public void SetPracticeActive(bool active)
        {
            if (!backgroundSource) return;
            if (active) backgroundSource.Pause();
            else backgroundSource.UnPause();
        }
    }
}
