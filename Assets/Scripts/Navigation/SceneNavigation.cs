using UnityEngine;
using UnityEngine.SceneManagement;

namespace RhythmRehab
{
    public sealed class SceneNavigation : MonoBehaviour
    {
        private static string settingsReturnScene = "MainMenu";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSettingsReturn() => settingsReturnScene = "MainMenu";
        public void Play()
        {
            if (IrisSceneTransition.IsBusy) return;
            SessionSelection.BeginBrowsing();
            IrisSceneTransition.Load("WellbeingAssessment");
        }
        public void Replay() => IrisSceneTransition.Load(SessionSelection.Configured ? "Gameplay" : "SongSelection");
        public void Settings()
        {
            if (IrisSceneTransition.IsBusy) return;
            string scene = SceneManager.GetActiveScene().name;
            if (scene != "Settings" && scene != "RhythmCalibration" && scene != "SensorCalibration" && scene != "SensorConnection") settingsReturnScene = scene;
            IrisSceneTransition.Load("Settings");
        }
        public void ReturnFromSettings() => IrisSceneTransition.Load(settingsReturnScene);
        public void RhythmCalibration() => IrisSceneTransition.Load("RhythmCalibration");
        public void SensorCalibration() => IrisSceneTransition.Load("SensorCalibration");
        public void SensorConnection() => IrisSceneTransition.Load("SensorConnection");
        public void Profile() { if (!PatientContext.IsGuest && FirebaseLoginSession.Instance && FirebaseLoginSession.Instance.IsSignedIn) IrisSceneTransition.Load("PerformanceHistory"); }
        public void Patients() => IrisSceneTransition.Load(FirebaseLoginSession.Instance && FirebaseLoginSession.Instance.IsSignedIn ? "PatientSelection" : "Login");
        public void Home() => IrisSceneTransition.Load("MainMenu");
        public void Exit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}

