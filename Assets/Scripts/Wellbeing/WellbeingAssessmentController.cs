using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class WellbeingAssessmentController : MonoBehaviour
    {
        [Header("Prototype message thresholds")]
        [SerializeField, Range(1,10)] private int highPainThreshold = 8;
        [SerializeField, Range(1,10)] private int highStressThreshold = 8;
        [Header("Saved assessment controls")]
        [SerializeField] private Slider painSlider, stressSlider;
        [SerializeField] private TMP_Text painValue, stressValue, painMessage, stressMessage, feedbackNotice;
        [SerializeField] private GameObject messageOverlay;
        [SerializeField] private TMP_Text messageTitle, messageBody;
        [SerializeField] private Image messageIcon;
        [SerializeField] private Sprite painAlertIcon, stressAlertIcon;
        private bool painAcknowledged, stressAcknowledged, showingPain, showingStress;
        public int Pain => Mathf.RoundToInt(painSlider.value);
        public int Stress => Mathf.RoundToInt(stressSlider.value);
        public bool MessageOpen => messageOverlay.activeSelf;
        public bool HighPain => Pain >= highPainThreshold;
        public bool HighStress => Stress >= highStressThreshold;
        private static readonly Color LowColour = new Color(.35f,.9f,.65f);
        private static readonly Color ModerateColour = new Color(1f,.78f,.3f);
        private static readonly Color HighColour = new Color(1f,.42f,.46f);

        private void Start()
        {
            messageOverlay.SetActive(false);
            Refresh(0);
        }
        public void Refresh(float value)
        {
            painAcknowledged = stressAcknowledged = false;
            painValue.text = $"{Pain} / 10";
            stressValue.text = $"{Stress} / 10";
            var painColour = RatingColour(Pain, HighPain);
            var stressColour = RatingColour(Stress, HighStress);
            painValue.color = painColour;
            stressValue.color = stressColour;
            string painHeading = HighPain ? "HIGH PAIN" : Pain >= 4 ? "MODERATE PAIN" : Pain == 0 ? "NO PAIN REPORTED" : "LOW PAIN";
            string stressHeading = HighStress ? "HIGH STRESS" : Stress >= 4 ? "MODERATE STRESS" : "LOW STRESS";
            painMessage.text = Message(painHeading, HighPain ? "Consider resting before continuing." :
                Pain >= 4 ? "Choose a height that feels comfortable." : "Move at a pace that feels comfortable.", painColour);
            stressMessage.text = Message(stressHeading, HighStress ? "Take a moment to relax before continuing." :
                Stress >= 4 ? "Take your time. Continue when you feel ready." : "Continue when you feel ready.", stressColour);
            feedbackNotice.text = "CHECK IN WITH YOURSELF\nChoose the ratings that describe how you feel. There are no right or wrong answers.";
        }
        private static Color RatingColour(int rating, bool high) => high ? HighColour : rating >= 4 ? ModerateColour : LowColour;
        private static string Message(string heading, string body, Color colour)
            => $"<color=#{ColorUtility.ToHtmlStringRGB(colour)}>{heading}</color>\n{body}";
        public void Continue()
        {
            if (IrisSceneTransition.IsBusy || MessageOpen) return;
            painAcknowledged = stressAcknowledged = false;
            ContinueAfterMessages();
        }
        private void ContinueAfterMessages()
        {
            if ((HighPain && !painAcknowledged) || (HighStress && !stressAcknowledged))
            {
                ShowMessage(HighPain && !painAcknowledged, HighStress && !stressAcknowledged);
                return;
            }
            painSlider.interactable = stressSlider.interactable = true;
            SessionWellbeing.Record(Pain,Stress,highPainThreshold,highStressThreshold);
            IrisSceneTransition.Load("SongSelection");
        }
        private void ShowMessage(bool pain, bool stress)
        {
            showingPain = pain;
            showingStress = stress;
            painSlider.interactable = stressSlider.interactable = false;
            messageTitle.text = pain && stress ? "HIGH PAIN AND STRESS REPORTED" :
                pain ? "HIGH PAIN REPORTED" : "HIGH STRESS REPORTED";
            messageTitle.color = HighColour;
            messageBody.text = pain && stress ?
                "Consider resting and take a moment to relax.\n\nYou can review your ratings or return to the menu.\n\nIf you continue, score and progress indicators will be hidden." :
                pain ? "Consider resting before continuing.\n\nYou can review your ratings or return to the menu.\n\nScore and progress indicators will be hidden.\nMove gently and stop if you feel discomfort." :
                "Take a moment to relax. Continue when you feel ready.\n\nYou can review your ratings or return to the menu.\n\nScore and progress indicators will be hidden.";
            messageIcon.sprite = pain ? painAlertIcon : stressAlertIcon;
            messageIcon.color = HighColour;
            messageOverlay.SetActive(true);
            ClearSelection();
        }
        public void Acknowledge()
        {
            if (!MessageOpen || IrisSceneTransition.IsBusy) return;
            if (showingPain) painAcknowledged = true;
            if (showingStress) stressAcknowledged = true;
            messageOverlay.SetActive(false);
            ContinueAfterMessages();
        }
        public void Review()
        {
            messageOverlay.SetActive(false);
            painAcknowledged = stressAcknowledged = false;
            painSlider.interactable = stressSlider.interactable = true;
            ClearSelection();
        }
        public void Skip()
        {
            if (IrisSceneTransition.IsBusy || MessageOpen) return;
            SessionWellbeing.Skip();
            IrisSceneTransition.Load("SongSelection");
        }
        public void Back()
        {
            if (IrisSceneTransition.IsBusy) return;
            SessionWellbeing.Reset();
            IrisSceneTransition.Load("MainMenu");
        }
        private static void ClearSelection() { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); }
    }
}
