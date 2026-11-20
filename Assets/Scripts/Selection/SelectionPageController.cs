using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhythmRehab
{
    public enum SelectionPage { Mode, Height, Difficulty }

    public sealed class SelectionPageController : MonoBehaviour
    {
        [SerializeField] private SelectionPage page;
        [SerializeField] private SongCatalog catalog;
        [SerializeField] private TMP_Text songContext;
        [SerializeField] private TMP_Text selectedLabel;
        [SerializeField] private Outline[] choiceOutlines;
        [SerializeField] private GameObject[] choiceBadges;
        [SerializeField] private ModeSelectionPreview modePreview;
        [SerializeField] private TMP_Text[] difficultyNoteCounts;
        [SerializeField] private TMP_Text[] targetAngleLabels;
        [SerializeField] private TMP_Text[] prescribedRepetitionLabels;
        [SerializeField] private Button nextPlanButton;
        [SerializeField] private TMP_Text guidedRepsLabel;
        [SerializeField] private TMP_Text rhythmStartLabel;
        private List<RehabilitationPlan> plans;
        private int planIndex;
        private bool loadingPlan;
        private string planError;
        private RehabilitationPlan SelectedPlan => plans != null && plans.Count > 0 ? plans[planIndex] : null;

        private void Start()
        {
            if (SessionSelection.Song == null) SessionSelection.SelectSong(catalog.songs[0]);
            var song = SessionSelection.Song;
            if (page == SelectionPage.Difficulty && songContext)
                songContext.text = song.audio ? $"{song.title}  ·  {song.artist}\nRecording: {song.recordingCredit}" : $"{song.title}  ·  {song.artist}  ·  {song.bpm:0} BPM";
            Refresh();
            if (page == SelectionPage.Height && !PatientContext.IsGuest)
                StartCoroutine(LoadPlans());
        }
        private IEnumerator LoadPlans()
        {
            loadingPlan = true;
            Refresh();
            var directory = PatientDirectory.Instance;
            if (directory)
                yield return directory.ReadPlans(PatientContext.PatientId, (result, error) => { plans = result; planError = error; });
            else
                planError = "Assigned plans are unavailable.";
            loadingPlan = false;
            Refresh();
        }
        public void NextPlan()
        {
            if (plans == null || plans.Count < 2) return;
            planIndex = (planIndex + 1) % plans.Count;
            Refresh();
        }
        public void Therapy()
        {
            if (IrisSceneTransition.IsBusy) return;
            SessionSelection.SelectMode(SessionMode.Therapy);
            Refresh();
            if (modePreview) modePreview.Show(SessionMode.Therapy);
        }
        public void Free()
        {
            if (IrisSceneTransition.IsBusy) return;
            SessionSelection.SelectMode(SessionMode.Free);
            Refresh();
            if (modePreview) modePreview.Show(SessionMode.Free);
        }
        public void ContinueMode()
        {
            if (page != SelectionPage.Mode || IrisSceneTransition.IsBusy || (modePreview && modePreview.IsTransitioning)) return;
            IrisSceneTransition.Load(SessionSelection.Mode == SessionMode.Therapy ? "ExerciseSelection" : "DifficultySelection");
        }
        public void Choose(int index)
        {
            if (index < 0 || index > 2 || page == SelectionPage.Mode) return;
            if (page == SelectionPage.Height) SessionSelection.SelectHeight((HeightLevel)index);
            else SessionSelection.SelectDifficulty((FreeDifficulty)index);
            Refresh();
        }
        private void Refresh()
        {
            if (page == SelectionPage.Difficulty && SessionSelection.Song != null && difficultyNoteCounts != null)
                for (int i = 0; i < difficultyNoteCounts.Length && i < 3; i++)
                    if (difficultyNoteCounts[i]) difficultyNoteCounts[i].text = SessionSelection.BuildProfile(SessionSelection.Song, SessionMode.Free, HeightLevel.Low, (FreeDifficulty)i).chart.Length + " notes";
            int index = page == SelectionPage.Mode ? (int)SessionSelection.Mode :
                page == SelectionPage.Height ? (int)SessionSelection.Height : (int)SessionSelection.Difficulty;
            for (int i = 0; i < choiceOutlines.Length; i++)
            {
                choiceOutlines[i].enabled = i == index;
                choiceBadges[i].SetActive(i == index);
            }
            selectedLabel.text = page == SelectionPage.Mode ?
                (SessionSelection.Mode == SessionMode.Therapy ? "THERAPY MODE  ·  Choose a target height next" : "FREE MODE  ·  Choose a difficulty next") :
                page == SelectionPage.Height ? $"{SessionSelection.Height.ToString().ToUpperInvariant()} HEIGHT  ·  One target for all notes" : $"SELECTED: {SessionSelection.Difficulty.ToString().ToUpperInvariant()}  ·  Varied heights between the guide lines.";
            if (page != SelectionPage.Height) return;
            var plan = SelectedPlan;
            if (nextPlanButton) nextPlanButton.gameObject.SetActive(plans != null && plans.Count > 1);
            if (guidedRepsLabel) guidedRepsLabel.text = SessionSelection.ShowGuidedReps ? "GUIDED REPS: ON" : "GUIDED REPS: OFF";
            if (rhythmStartLabel) rhythmStartLabel.text = "START REHAB";
            RefreshTargetAngles(plan);
            if (prescribedRepetitionLabels != null)
            {
                string prescription = plan == null ? "No prescribed sets or reps" : $"{plan.sets} sets × {plan.repetitions} reps";
                foreach (var label in prescribedRepetitionLabels)
                    if (label) label.text = prescription;
            }
            if (loadingPlan) selectedLabel.text = "Loading assigned plans…";
            else if (planError != null) selectedLabel.text = planError;
            else if (plan != null)
                selectedLabel.text = $"{plan.name}  ·  {plan.TargetDegrees(SessionSelection.Height):0.#}° SHIN TILT  ·  {plan.sets} × {plan.repetitions} TARGET REPS";
            else if (!PatientContext.IsGuest) selectedLabel.text = "Practice · No active leg-raise plan";
        }

        private void RefreshTargetAngles(RehabilitationPlan plan)
        {
            if (targetAngleLabels == null) return;
            var sensor = UsbSensorConnection.Instance;
            var calibration = sensor ? sensor.Calibration : null;
            for (int i = 0; i < targetAngleLabels.Length; i++)
            {
                var label = targetAngleLabels[i];
                if (!label) continue;
                if (plan != null)
                    label.text = $"{plan.TargetDegrees((HeightLevel)i):0.#}° shin tilt";
                else if (calibration != null && calibration.IsReady)
                {
                    float degrees = new ChartNote { lane = i }.TargetHeight * calibration.RangeDegrees;
                    label.text = $"{degrees:0.#}° shin tilt";
                }
                else
                    label.text = "Calibrate for angle";
            }
        }

        public void ToggleGuidedReps()
        {
            if (page != SelectionPage.Height || IrisSceneTransition.IsBusy) return;
            SessionSelection.SetGuidedReps(!SessionSelection.ShowGuidedReps);
            Refresh();
        }

        public void BeginGame()
        {
            if (IrisSceneTransition.IsBusy) return;
            if (page == SelectionPage.Height && (loadingPlan || planError != null)) return;
            var plan = page == SelectionPage.Height ? SelectedPlan : null;
            if (plan != null && plan.patientId != PatientContext.PatientId) return;
            bool showGuidance = page == SelectionPage.Height && SessionSelection.ShowGuidedReps;
            SessionSelection.SelectMode(page == SelectionPage.Height ? SessionMode.Therapy : SessionMode.Free);
            SessionSelection.AssignPlan(plan);
            SessionSelection.SetGuidedReps(showGuidance);
            SessionSelection.Confirm();
            IrisSceneTransition.Load(page == SelectionPage.Height ? "DrawingActivity" : "Gameplay");
        }
        public void Back()
        {
            if (modePreview && modePreview.IsOpen) { modePreview.Close(); return; }
            IrisSceneTransition.Load(page == SelectionPage.Mode ? "SongSelection" : "ModeSelection");
        }
        public void Home() => IrisSceneTransition.Load("MainMenu");
    }
}

