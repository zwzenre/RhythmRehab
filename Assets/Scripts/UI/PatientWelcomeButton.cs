using TMPro;
using UnityEngine;

namespace RhythmRehab
{
    public sealed class PatientWelcomeButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text welcomeLabel;
        private void OnEnable() { PatientContext.Changed += Refresh; Refresh(); }
        private void OnDisable() => PatientContext.Changed -= Refresh;
        private void Refresh()
        {
            if (welcomeLabel) welcomeLabel.text = "<size=22>Welcome,</size>\n" + PatientContext.DisplayName;
        }
    }
}
