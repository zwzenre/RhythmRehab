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
        private bool practiceActive, songPreviewActive;

        private void Start()
        {
            var settings = LocalSettingsStore.Default.Load(out _);
            ApplyVolumes(settings.musicVolume, settings.sfxVolume);
            if (backgroundSource && backgroundTracks != null && backgroundTracks.Length > 0)
            {
                backgroundSource.clip = backgroundTracks[Random.Range(0, backgroundTracks.Length)];
                backgroundSource.Play();
                UpdateBackgroundPause();
            }
        }

        public void ApplyVolumes(float music, float sfx)
        {
            if (backgroundSource) backgroundSource.volume = AudioVolume.MusicGain(music);
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
            practiceActive = active; UpdateBackgroundPause();
        }
        public void SetSongPreviewActive(bool active)
        {
            songPreviewActive = active; UpdateBackgroundPause();
        }
        private void UpdateBackgroundPause()
        {
            if (!backgroundSource) return;
            if (practiceActive || songPreviewActive) backgroundSource.Pause();
            else backgroundSource.UnPause();
        }
    }
}
