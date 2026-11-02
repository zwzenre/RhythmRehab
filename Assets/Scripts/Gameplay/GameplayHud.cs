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
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private ScorePulse scorePulse;
        [SerializeField] private JudgementTextFeedback judgementMotion;
        [Header("Before gameplay")]
        [SerializeField] private CanvasGroup readyOverlay;
        [SerializeField, Min(.1f)] private float readyDuration = 3f;
        private float feedbackUntil;
        private int lastRemainingSeconds = -1;

        public IEnumerator ShowReady()
        {
            if (!readyOverlay) yield break;
            readyOverlay.gameObject.SetActive(true); readyOverlay.alpha = 1;
            readyOverlay.blocksRaycasts = true;
            yield return new WaitForSecondsRealtime(readyDuration);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .25f)
            {
                readyOverlay.alpha = 1 - Mathf.SmoothStep(0, 1, t);
                yield return null;
            }
            readyOverlay.alpha = 0; readyOverlay.blocksRaycasts = false;
            readyOverlay.gameObject.SetActive(false);
        }

        public void Initialize(SessionSummary summary, string song, string mode, float bpm)
        {
            bool reduced = summary.wellbeing != null && summary.wellbeing.reducedPerformanceFeedback;
            foreach (var indicator in performanceIndicators) if (indicator) indicator.SetActive(!reduced);
            string message = SessionWellbeing.GameplayMessage(summary.wellbeing);
            if (wellbeingGameplayTip && !string.IsNullOrEmpty(message)) wellbeingGameplayTip.text = message;
            if (songLabel) songLabel.text = song.ToUpperInvariant();
            if (modeLabel) modeLabel.text = $"{mode}\n{bpm:0} BPM";
            SetPauseMenu(false); RefreshScore(summary, 0); lastRemainingSeconds = -1;
        }
        public void SetPauseMenu(bool show) => pausePanel.SetActive(show);
        public void ShowResumePrompt() => statusLabel.text = "HOLD SPACE AT THE TARGET HEIGHT TO RESUME";
        public void ShowHeight(float height, bool continuous, float tolerance)
        {
            if (continuous) { heightLabel.text = $"HEIGHT\n{height:P0}"; return; }
            int lane = Mathf.Clamp(Mathf.RoundToInt(height * 2), 0, 2);
            string level = lane == 0 ? "LOW" : lane == 1 ? "MID" : "HIGH";
            heightLabel.text = (Mathf.Abs(height - lane * .5f) <= tolerance ? level : "BETWEEN HEIGHTS") + $"\n{height:P0}";
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
            statusLabel.text = runner.ActiveHoldIndex >= 0 ? $"KEEP SPACE HELD • MAINTAIN HEIGHT • {runner.ActiveHold.Progress(time):P0}" :
                "MOUSE ↑↓ HEIGHT    •    SPACE TAP / HOLD    •    ESC PAUSE";
            if (!judgementMotion && Time.unscaledTime > feedbackUntil) feedbackLabel.text = "";
            progressFill.fillAmount = Mathf.Clamp01(time / runner.EndTime);
            if (!remainingTimeLabel) return;
            int seconds = Mathf.CeilToInt(Mathf.Max(0, runner.EndTime - Mathf.Max(0, time)));
            if (seconds == lastRemainingSeconds) return;
            lastRemainingSeconds = seconds; remainingTimeLabel.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
