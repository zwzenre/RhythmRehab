using TMPro;
using System.Collections;
using UnityEngine;

namespace RhythmRehab
{
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreLabel, accuracyLabel, comboLabel, feedbackLabel, statusLabel, heightLabel, songLabel, modeLabel;
        [SerializeField] private UnityEngine.UI.Image progressFill;
        [SerializeField] private TMP_Text remainingTimeLabel;
        [SerializeField] private GameObject[] performanceIndicators;
        [SerializeField] private TMP_Text wellbeingGameplayTip;
        [SerializeField] private TMP_Text wellbeingScenarioTitle;
        [SerializeField] private UnityEngine.UI.Image carePanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject pauseActions, endSessionOptions, discardConfirmation;
        [SerializeField] private PauseGameplaySettings pauseSettings;
        [SerializeField] private ScorePulse scorePulse;
        [SerializeField] private JudgementTextFeedback judgementMotion;
        [Header("Before gameplay")]
        [SerializeField] private CanvasGroup readyOverlay;
        [SerializeField, Min(.1f)] private float readyDuration = 3f;
        private float feedbackUntil;
        private int lastRemainingSeconds = -1;
        private string hitKeyLabel = "SPACE";

        public IEnumerator ShowReady(float totalSeconds = -1)
        {
            if (!readyOverlay) yield break;
            readyOverlay.gameObject.SetActive(true); readyOverlay.alpha = 1;
            readyOverlay.blocksRaycasts = true;
            yield return new WaitForSecondsRealtime(totalSeconds < 0 ? readyDuration : Mathf.Max(0, totalSeconds - .25f));
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .25f)
            {
                readyOverlay.alpha = 1 - Mathf.SmoothStep(0, 1, t);
                yield return null;
            }
            HideReady();
        }

        public void HideReady()
        {
            if (!readyOverlay) return;
            readyOverlay.alpha = 0; readyOverlay.blocksRaycasts = false;
            readyOverlay.gameObject.SetActive(false);
        }

        public void Initialize(SessionSummary summary, string song, string mode, float bpm)
        {
            hitKeyLabel = HitControls.Label(LocalSettingsStore.Default.Load(out _));
            bool reduced = summary.wellbeing != null && summary.wellbeing.reducedPerformanceFeedback;
            foreach (var indicator in performanceIndicators) if (indicator) indicator.SetActive(!reduced);
            string message = SessionWellbeing.GameplayMessage(summary.wellbeing);
            if (wellbeingGameplayTip)
            {
                bool hasMessage = !string.IsNullOrEmpty(message);
                wellbeingGameplayTip.gameObject.SetActive(hasMessage);
                if (hasMessage) wellbeingGameplayTip.text = message;
            }
            if(carePanel)
            {
                carePanel.enabled = false;
                carePanel.raycastTarget = false;
            }
            if(wellbeingScenarioTitle)
            {
                wellbeingScenarioTitle.gameObject.SetActive(reduced);
                wellbeingScenarioTitle.text=SessionWellbeing.ScenarioLabel(summary.wellbeing).ToUpperInvariant();
            }
            if (songLabel) songLabel.text = song.ToUpperInvariant();
            if (modeLabel)
            {
                string details = mode;
                if (SessionSelection.Configured)
                    details = SessionSelection.Mode == SessionMode.Therapy
                        ? $"THERAPY MODE  ·  {SessionSelection.Height.ToString().ToUpperInvariant()} HEIGHT"
                        : $"FREE MODE  ·  {SessionSelection.Difficulty.ToString().ToUpperInvariant()}";
                modeLabel.text = details;
            }
            SetPauseMenu(false); RefreshScore(summary, 0); lastRemainingSeconds = -1;
        }
        public void SetPauseMenu(bool show)
        {
            pausePanel.SetActive(show);
            ShowPauseActions();
        }
        public void ShowPauseActions()
        {
            if (!pauseActions) return;
            pauseActions.SetActive(true);
            endSessionOptions.SetActive(false);
            discardConfirmation.SetActive(false);
        }
        public void ShowEndOptions()
        {
            if (!endSessionOptions) return;
            pauseActions.SetActive(false);
            discardConfirmation.SetActive(false);
            endSessionOptions.SetActive(true);
        }
        public void ShowDiscardConfirmation()
        {
            if (!discardConfirmation) return;
            endSessionOptions.SetActive(false);
            discardConfirmation.SetActive(true);
        }
        public bool IsDiscardConfirmationOpen => discardConfirmation && discardConfirmation.activeSelf;
        public bool BackFromEndOptions()
        {
            if (discardConfirmation && discardConfirmation.activeSelf) { ShowEndOptions(); return true; }
            if (!endSessionOptions || !endSessionOptions.activeSelf) return false;
            ShowPauseActions();
            return true;
        }
        public void OpenPauseSettings() { if (pauseSettings) pauseSettings.Open(); }
        public bool ClosePauseSettings()
        {
            if (!pauseSettings || !pauseSettings.gameObject.activeSelf) return false;
            pauseSettings.Back();
            return true;
        }
        public void ShowResumePrompt() => statusLabel.text = UsbSensorConnection.Instance && UsbSensorConnection.Instance.InputEnabled
            ? "RETURN TO THE TARGET HEIGHT TO RESUME" : $"HOLD {hitKeyLabel} AT THE TARGET HEIGHT TO RESUME";
        public void ShowSensorUnavailable() => statusLabel.text = "SENSOR DATA LOST — OPEN PAUSE SETTINGS TO RECONNECT OR SWITCH CONTROLS.";
        public void ShowHeight(float height, bool continuous, float tolerance)
        {
            if (continuous) { heightLabel.text = $"HEIGHT\n{height:P0}"; return; }
            float lanePosition = Mathf.InverseLerp(ChartNote.LowTargetHeight, 1f, height) * 2;
            int lane = Mathf.RoundToInt(lanePosition);
            string level = lane == 0 ? "LOW" : lane == 1 ? "MID" : "HIGH";
            float target = new ChartNote { lane = lane }.TargetHeight;
            string status = height < ChartNote.LowTargetHeight - tolerance ? "BELOW LOW" : "BETWEEN HEIGHTS";
            heightLabel.text = (Mathf.Abs(height - target) <= tolerance ? level : status) + $"\n{height:P0}";
        }
        public void RefreshScore(SessionSummary summary, int combo)
        { scoreLabel.text = summary.score.ToString("N0"); accuracyLabel.text = summary.Judged == 0 ? "—" : summary.Accuracy.ToString("P0"); comboLabel.text = $"{combo} COMBO"; }
        public void ShowFeedback(string text, Color colour, HitJudgement judgement)
        {
            feedbackLabel.text = text.ToUpperInvariant(); feedbackLabel.color = colour; feedbackUntil = Time.unscaledTime + .55f;
            if (judgementMotion) judgementMotion.Show(feedbackLabel.text, colour, judgement);
        }
        public void PunchScore() { if (scorePulse) scorePulse.Punch(); }
        public void UpdateTimeline(GameplayChartRunner runner, float time)
        {
            bool sensor = UsbSensorConnection.Instance && UsbSensorConnection.Instance.InputEnabled;
            statusLabel.text = runner.ActiveHoldIndex >= 0 ? $"{(sensor ? "MAINTAIN HEIGHT" : "KEEP " + hitKeyLabel + " HELD • MAINTAIN HEIGHT")} • {runner.ActiveHold.Progress(time):P0}" :
                (sensor ? "SHIN SENSOR HEIGHT    •    AUTO HIT    •    ESC PAUSE" : $"MOUSE ↑↓ HEIGHT    •    {hitKeyLabel} TAP / HOLD    •    ESC PAUSE");
            if (!judgementMotion && Time.unscaledTime > feedbackUntil) feedbackLabel.text = "";
            progressFill.fillAmount = Mathf.Clamp01(time / runner.EndTime);
            if (!remainingTimeLabel) return;
            int seconds = Mathf.CeilToInt(Mathf.Max(0, runner.EndTime - Mathf.Max(0, time)));
            if (seconds == lastRemainingSeconds) return;
            lastRemainingSeconds = seconds; remainingTimeLabel.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
