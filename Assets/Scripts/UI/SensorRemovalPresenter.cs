using System.Collections;
using UnityEngine;

namespace RhythmRehab
{
    public sealed class SensorRemovalPresenter : MonoBehaviour
    {
        [SerializeField, Min(1)] private float displaySeconds = 6;
        [SerializeField] private TMPro.TMP_Text nextLabel;
        private static string nextScene = "Results";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => nextScene = "Results";

        public static bool ShowBefore(string destination)
        {
            if (IrisSceneTransition.IsBusy || !Application.CanStreamedLevelBeLoaded(destination)) return false;
            nextScene = destination;
            return IrisSceneTransition.Load("SensorRemoval");
        }

        private IEnumerator Start()
        {
            string destination = nextScene;
            nextScene = "Results";
            if (nextLabel) nextLabel.text = destination == "Results" ? "Session Summary opens shortly" : "Returning to the main menu shortly";
            while (IrisSceneTransition.IsBusy) yield return null;
            yield return new WaitForSecondsRealtime(displaySeconds);
            while (IrisSceneTransition.IsBusy) yield return null;
            IrisSceneTransition.Load(destination);
        }
    }
}
