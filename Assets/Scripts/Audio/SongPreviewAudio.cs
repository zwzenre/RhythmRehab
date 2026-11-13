using System.Collections;
using UnityEngine;

namespace RhythmRehab
{
    // One authored source is reused for every card preview.
    public sealed class SongPreviewAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource previewSource;
        [SerializeField] private UIScreenAudio screenAudio;
        [SerializeField, Min(.05f)] private float fadeSeconds = .35f;
        private Coroutine routine;
        public SongEntry CurrentSong { get; private set; }
        public bool IsPlaying => previewSource && previewSource.isPlaying;
        public AudioSource Source => previewSource;

        public void Play(SongEntry song)
        {
            StopPreview();
            if (song == null || !song.audio || !previewSource) return;
            CurrentSong = song;
            if (screenAudio) screenAudio.SetSongPreviewActive(true);
            routine = StartCoroutine(Preview(song));
        }
        private IEnumerator Preview(SongEntry song)
        {
            song.audio.LoadAudioData();
            while (song.audio.loadState == AudioDataLoadState.Loading) yield return null;
            if (song.audio.loadState != AudioDataLoadState.Loaded) { StopPreview(); yield break; }
            previewSource.clip = song.audio; previewSource.loop = false;
            float start = Mathf.Clamp(song.previewStartSeconds, 0, Mathf.Max(0, song.audio.length - 1));
            float length = Mathf.Min(Mathf.Max(3, song.previewDurationSeconds), song.audio.length - start);
            float volume = AudioVolume.MusicGain(LocalSettingsStore.Default.Load(out _).musicVolume);
            while (true)
            {
                previewSource.volume = 0; previewSource.time = start; previewSource.Play();
                for (float time = 0; time < length; time += Time.unscaledDeltaTime)
                {
                    float fade = Mathf.Min(Mathf.Clamp01(time / fadeSeconds), Mathf.Clamp01((length - time) / fadeSeconds));
                    previewSource.volume = volume * Mathf.SmoothStep(0, 1, fade);
                    yield return null;
                }
                previewSource.Stop();
            }
        }
        public void StopPreview()
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
            if (previewSource) { previewSource.Stop(); previewSource.clip = null; }
            CurrentSong = null;
            if (screenAudio) screenAudio.SetSongPreviewActive(false);
        }
        private void OnDisable() => StopPreview();
    }
}
