using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class LoginPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_InputField email, password;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button signIn;
        public void Login()
        {
            var session = FirebaseLoginSession.Instance;
            if (!session || session.IsSigningIn || IrisSceneTransition.IsBusy) return;
            if (string.IsNullOrWhiteSpace(email.text) || string.IsNullOrEmpty(password.text))
            { status.text = "Enter your email and password."; return; }
            status.text = "Signing in…"; signIn.interactable = false;
            session.SignIn(email.text, password.text, error =>
            {
                if (!this) return;
                signIn.interactable = true; status.text = error ?? "Signed in";
                if (error == null) IrisSceneTransition.Load("PatientSelection");
            });
            password.text = "";
        }
        public void Guest()
        {
            if (IrisSceneTransition.IsBusy) return;
            IrisSceneTransition.Load("MainMenu");
            if (FirebaseLoginSession.Instance) FirebaseLoginSession.Instance.SignOut();
        }
    }
}
