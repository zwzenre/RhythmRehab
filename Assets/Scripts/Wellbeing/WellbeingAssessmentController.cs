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
        private bool painAcknowledged, stressAcknowledged, showingPain;
        public int Pain => Mathf.RoundToInt(painSlider.value);
        public int Stress => Mathf.RoundToInt(stressSlider.value);
        public bool MessageOpen => messageOverlay.activeSelf;
        public bool HighPain => Pain >= highPainThreshold;
        public bool HighStress => Stress >= highStressThreshold;
        private static readonly Color Purple = new Color(.72f,.36f,1), Cyan = new Color(.15f,.8f,1);

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
            painMessage.text = HighPain ? "HIGH PAIN\nPlease consider resting before continuing." : Pain >= 4 ?
                "MODERATE PAIN\nTake your time and choose what feels comfortable." : "LOW PAIN\nYour rating will be saved with this session.";
            stressMessage.text = HighStress ? "HIGH STRESS\nTake a moment to relax before continuing." : Stress >= 4 ?
                "MODERATE STRESS\nTake a moment and continue when ready." : "LOW STRESS\nContinue when you feel ready.";
            feedbackNotice.text = "CHECK IN WITH YOURSELF\nChoose the ratings that best describe how you feel right now. There are no right or wrong answers.";
        }
        public void Continue()
        {
            if (IrisSceneTransition.IsBusy || MessageOpen) return;
            painAcknowledged = stressAcknowledged = false;
            ContinueAfterMessages();
        }
        private void ContinueAfterMessages()
        {
            if (HighPain && !painAcknowledged) { ShowMessage(true); return; }
            if (HighStress && !stressAcknowledged) { ShowMessage(false); return; }
            painSlider.interactable = stressSlider.interactable = true;
            SessionWellbeing.Record(Pain,Stress,highPainThreshold,highStressThreshold);
            IrisSceneTransition.Load("SongSelection");
        }
        private void ShowMessage(bool pain)
        {
            showingPain = pain;
            painSlider.interactable = stressSlider.interactable = false;
            messageTitle.text = pain ? "YOU REPORTED A HIGH PAIN LEVEL" : "YOU REPORTED A HIGH STRESS LEVEL";
            messageBody.text = pain ?
                "Please consider resting before continuing.\n\nYou may review your ratings or return to the main menu.\nYour safety and comfort are our priority.\n\nIf you continue, performance indicators will be hidden." :
                "Take a moment to relax before continuing.\n\nYou may proceed when you feel ready or review your ratings.\n\nYour score and progress will be hidden during the session to reduce pressure.";
            messageIcon.sprite = pain ? painAlertIcon : stressAlertIcon;
            messageIcon.color = pain ? Purple : Cyan;
            messageOverlay.SetActive(true);
            ClearSelection();
        }
        public void Acknowledge()
        {
            if (!MessageOpen || IrisSceneTransition.IsBusy) return;
            if (showingPain) painAcknowledged = true; else stressAcknowledged = true;
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
