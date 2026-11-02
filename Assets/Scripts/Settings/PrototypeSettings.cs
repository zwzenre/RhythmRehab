using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class PrototypeSettings : MonoBehaviour
    {
        [SerializeField] private Slider beatVolume;
        [SerializeField] private Slider sfxVolume;
        [SerializeField] private TMP_Text beatValue;
        [SerializeField] private TMP_Text sfxValue;

        private void Start()
        {
            var settings = LocalSettingsStore.Default.Load(out var error);
            if (error != null) Debug.LogWarning("Settings could not be loaded: " + error);
            beatVolume.SetValueWithoutNotify(settings.beatVolume);
            sfxVolume.SetValueWithoutNotify(settings.sfxVolume);
            UpdateValues();
        }

        public void SaveVolumes(float value)
        {
            if (!LocalSettingsStore.Default.TrySaveVolumes(beatVolume.value, sfxVolume.value, out var error))
                Debug.LogWarning("Audio settings could not be saved: " + error);
            var audio = FindAnyObjectByType<UIScreenAudio>();
            if (audio) audio.ApplyVolumes(beatVolume.value, sfxVolume.value);
            UpdateValues();
        }

        private void UpdateValues()
        {
            beatValue.text = beatVolume.value.ToString("P0");
            sfxValue.text = sfxVolume.value.ToString("P0");
        }


    }
}
