using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class PauseGameplaySettings : MonoBehaviour
    {
        [SerializeField] private PrototypeGameplay gameplay;
        [SerializeField] private GameObject pauseActions;
        [SerializeField] private GameObject preferences, sensorSetup;
        [SerializeField] private GameObject calibrationModelStage;
        [SerializeField] private Slider music, beat, ui;
        [SerializeField] private TMP_Text musicValue, beatValue, uiValue, inputStatus;

        public void Open()
        {
            var settings = LocalSettingsStore.Default.Load(out _);
            music.SetValueWithoutNotify(settings.musicVolume);
            beat.SetValueWithoutNotify(settings.beatVolume);
            ui.SetValueWithoutNotify(settings.sfxVolume);
            RefreshVolumes();
            BackToPreferences();
            pauseActions.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (calibrationModelStage) calibrationModelStage.SetActive(false);
            gameObject.SetActive(false);
            pauseActions.SetActive(true);
        }

        public void OpenSensorSetup()
        {
            preferences.SetActive(false);
            sensorSetup.SetActive(true);
            if (calibrationModelStage) calibrationModelStage.SetActive(true);
        }

        public void BackToPreferences()
        {
            if (calibrationModelStage) calibrationModelStage.SetActive(false);
            sensorSetup.SetActive(false);
            preferences.SetActive(true);
            ShowControls();
        }

        public void Back()
        {
            if (sensorSetup.activeSelf) BackToPreferences();
            else Close();
        }

        public void ChangeVolume(float value)
        {
            gameplay.Audio.ApplyVolumes(music.value, beat.value);
            var screenAudio = FindAnyObjectByType<UIScreenAudio>();
            if (screenAudio) screenAudio.ApplyVolumes(music.value, ui.value);
            if (!LocalSettingsStore.Default.TrySaveAudio(music.value, beat.value, ui.value, out var error))
                Debug.LogWarning("Audio settings could not be saved: " + error);
            RefreshVolumes();
        }

        public void SelectMouse() { if (gameplay.SelectControls(false)) ShowControls(); }
        public void SelectSensor()
        {
            if (gameplay.SelectControls(true)) ShowControls();
            else inputStatus.text = "Sensor unavailable. Open Sensor Setup to connect and calibrate.";
        }

        private void ShowControls() => inputStatus.text = gameplay.UsingSensor
            ? "ACTIVE: SENSOR  •  Match the height as notes reach the line."
            : "ACTIVE: MOUSE + KEYBOARD  •  " + HitControls.Label(LocalSettingsStore.Default.Load(out _)) + " TAP / HOLD";

        private void RefreshVolumes()
        {
            musicValue.text = music.value.ToString("P0");
            beatValue.text = beat.value.ToString("P0");
            uiValue.text = ui.value.ToString("P0");
        }
    }
}
