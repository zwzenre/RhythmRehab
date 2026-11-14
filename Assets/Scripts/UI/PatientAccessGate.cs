using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class PatientAccessGate : MonoBehaviour
    {
        [SerializeField] private Button profileButton;
        [SerializeField] private CanvasGroup profileOpacity;
        [SerializeField] private bool protectedPage;
        private void LateUpdate()
        {
            bool signedIn = FirebaseLoginSession.Instance && FirebaseLoginSession.Instance.IsSignedIn;
            if (profileButton) profileButton.interactable = signedIn && !PatientContext.IsGuest;
            if (profileOpacity) profileOpacity.alpha = signedIn && !PatientContext.IsGuest ? 1 : .4f;
            if (protectedPage && !signedIn && !IrisSceneTransition.IsBusy) IrisSceneTransition.Load("Login");
        }
        public void Logout()
        {
            if (IrisSceneTransition.IsBusy) return;
            IrisSceneTransition.Load("MainMenu");
            if (FirebaseLoginSession.Instance) FirebaseLoginSession.Instance.SignOut();
        }
    }
}
