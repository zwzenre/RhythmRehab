using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class PrototypeSettings : MonoBehaviour
    {
        [SerializeField] private Slider beatVolume;
        [SerializeField] private Slider musicVolume;
        [SerializeField] private Slider sfxVolume;
        [SerializeField] private TMP_Text beatValue;
        [SerializeField] private TMP_Text musicValue;
        [SerializeField] private TMP_Text sfxValue;
        [SerializeField] private TMP_Text primaryKeyLabel, secondaryKeyLabel, keyCaptureStatus;
        private LocalSettings keySettings;
        private int capturing = -1;

        private void Start()
        {
            var settings = LocalSettingsStore.Default.Load(out var error);
            if (error != null) Debug.LogWarning("Settings could not be loaded: " + error);
            beatVolume.SetValueWithoutNotify(settings.beatVolume);
            musicVolume.SetValueWithoutNotify(settings.musicVolume);
            sfxVolume.SetValueWithoutNotify(settings.sfxVolume);
            keySettings = settings;
            RefreshKeys();
            UpdateValues();
        }

        public void BindPrimary() => BeginCapture(0);
        public void BindSecondary() => BeginCapture(1);
        private void BeginCapture(int slot)
        {
            capturing = slot;
            if (UnityEngine.EventSystems.EventSystem.current) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            keyCaptureStatus.text = "Press a key · Esc cancels" + (slot == 1 ? " · Backspace clears" : "");
        }
        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (capturing < 0 || keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { capturing = -1; RefreshKeys(); return; }
            foreach (var key in keyboard.allKeys)
            {
                if (!key.wasPressedThisFrame) continue;
                int value = capturing == 1 && key.keyCode == UnityEngine.InputSystem.Key.Backspace ? 0 : (int)key.keyCode;
                int primary = capturing == 0 ? value : keySettings.hitKeyPrimary;
                int secondary = capturing == 1 ? value : keySettings.hitKeySecondary;
                if (!LocalSettingsStore.Default.TrySaveHitKeys(primary, secondary, out var error))
                { keyCaptureStatus.text = error; return; }
                keySettings.hitKeyPrimary = primary; keySettings.hitKeySecondary = secondary;
                capturing = -1; RefreshKeys(); return;
            }
        }
        public void ResetKeys()
        {
            if (!LocalSettingsStore.Default.TrySaveHitKeys((int)UnityEngine.InputSystem.Key.Space, 0, out var error))
            { keyCaptureStatus.text = error; return; }
            keySettings.hitKeyPrimary = (int)UnityEngine.InputSystem.Key.Space; keySettings.hitKeySecondary = 0;
            capturing = -1; RefreshKeys();
        }
        private void RefreshKeys()
        {
            if (!primaryKeyLabel) return;
            primaryKeyLabel.text = HitControls.Label(keySettings.hitKeyPrimary);
            secondaryKeyLabel.text = keySettings.hitKeySecondary == 0 ? "ADD KEY" : HitControls.Label(keySettings.hitKeySecondary);
            keyCaptureStatus.text = "Left / right click always hits · Select a key to rebind it";
        }

        public void SaveVolumes(float value)
        {
            if (!LocalSettingsStore.Default.TrySaveAudio(musicVolume.value, beatVolume.value, sfxVolume.value, out var error))
                Debug.LogWarning("Audio settings could not be saved: " + error);
            var audio = FindAnyObjectByType<UIScreenAudio>();
            if (audio) audio.ApplyVolumes(musicVolume.value, sfxVolume.value);
            UpdateValues();
        }

        private void UpdateValues()
        {
            beatValue.text = beatVolume.value.ToString("P0");
            musicValue.text = musicVolume.value.ToString("P0");
            sfxValue.text = sfxVolume.value.ToString("P0");
        }


    }
}
