using TMPro;
using UnityEngine;

namespace RhythmRehab
{
    public sealed class ResultsPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text overall;
        [SerializeField] private TMP_Text grade;
        [SerializeField] private TMP_Text performance;
        [SerializeField] private TMP_Text judgements;
        [SerializeField] private TMP_Text recommendation;
        [SerializeField] private TMP_Text sessionContext;

        private void Start()
        {
            var s = SessionSummary.Last;
            if (s == null)
            {
                overall.text = "—";
                grade.text = "—";
                performance.text = "No completed session yet.\n\nChoose PLAY AGAIN to try the prototype.";
                judgements.text = "PERFECT   —     GREAT   —\n\nGOOD       —     NOTES TO PRACTISE   —";
                recommendation.text = "NO COMPLETED SESSION\n\nComplete a session to view feedback.\nKnee-joint ROM is not measured.";
                return;
            }
            overall.text = s.Judged == 0 ? "—" : s.Accuracy.ToString("P1");
            if (sessionContext != null) sessionContext.text = $"{s.songTitle}  ·  {s.modeLabel}  ·  {s.bpm:0} BPM";
            grade.text = s.Judged == 0 ? "—" : s.Accuracy >= 0.9f ? "A" : s.Accuracy >= 0.75f ? "B" : s.Accuracy >= 0.5f ? "C" : "D";
            var duration = System.TimeSpan.FromSeconds(s.duration);
            performance.text = $"SESSION  ·  {(s.endedEarly ? "ENDED EARLY" : "COMPLETED")}\n\nCompletion       {s.Completion:P0} ({s.Judged}/{s.totalNotes} notes)\n\nDuration           {(int)duration.TotalMinutes:00}:{duration.Seconds:00}\n\nBest combo       {s.bestCombo}";
            judgements.text = $"<color=#FFD126>PERFECT</color>     {s.perfect}        <color=#B34DFF>GREAT</color>     {s.great}\n\n<color=#3399FF>GOOD</color>           {s.good}        <color=#A6BDE0>NOTES TO PRACTISE</color>   {s.miss}";
            string scenario = "Scenario: " + SessionWellbeing.ScenarioLabel(s.wellbeing);
            recommendation.text = (s.sensorHeightControl ? "SENSOR DATA  ·  SHIN SENSOR" : "MOUSE + KEYBOARD") + "\n" + scenario + "\n\nSaved locally for review.";
            if (!string.IsNullOrEmpty(s.historySaveError))
                recommendation.text = scenario + "\nHISTORY NOT SAVED\nCheck disk access before your next session.";
        }
    }
}
