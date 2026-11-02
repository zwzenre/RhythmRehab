using UnityEngine;
using UnityEngine.SceneManagement;

namespace RhythmRehab
{
    public sealed class SceneNavigation : MonoBehaviour
    {
        public void Play()
        {
            if (IrisSceneTransition.IsBusy) return;
            SessionSelection.BeginBrowsing();
            IrisSceneTransition.Load("WellbeingAssessment");
        }
        public void Replay() => IrisSceneTransition.Load(SessionSelection.Configured ? "Gameplay" : "SongSelection");
        public void Settings() => IrisSceneTransition.Load("Settings");
        public void RhythmCalibration() => IrisSceneTransition.Load("RhythmCalibration");
        public void SensorCalibration() => IrisSceneTransition.Load("SensorCalibration");
        public void SensorConnection() => IrisSceneTransition.Load("SensorConnection");
        public void Profile() => IrisSceneTransition.Load("PerformanceHistory");
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

