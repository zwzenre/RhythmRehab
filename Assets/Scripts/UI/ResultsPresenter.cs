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
                recommendation.text = "MOUSE PROTOTYPE\n\nSensor movement and ROM\nare not measured in this mode.";
                return;
            }
            overall.text = s.Accuracy.ToString("P0");
            if (sessionContext != null) sessionContext.text = $"{s.songTitle}  ·  {s.modeLabel}  ·  {s.bpm:0} BPM";
            grade.text = s.Accuracy >= 0.9f ? "A" : s.Accuracy >= 0.75f ? "B" : s.Accuracy >= 0.5f ? "C" : "D";
            var duration = System.TimeSpan.FromSeconds(s.duration);
            performance.text = $"Total score                         {s.score:N0}\n\nOverall accuracy                {s.Accuracy:P0}\n\nCompletion rate                 {s.Completion:P0}\n\nBest combo                         {s.bestCombo}\n\nDuration                              {duration.Minutes:00}:{duration.Seconds:00}\n\nAverage ROM                      Not measured";
            judgements.text = $"<color=#49DDF4>PERFECT</color>     {s.perfect}        <color=#58E89A>GREAT</color>     {s.great}\n\n<color=#FFD15A>GOOD</color>           {s.good}        <color=#A6BDE0>NOTES TO PRACTISE</color>   {s.miss}";
            recommendation.text = "MOUSE PROTOTYPE — no sensor data.\nROM and difficulty are not assessed.\n\nAccuracy weights: Perfect 100% · Great 80%\nGood 50% · Notes to practise 0%.";
            if (!string.IsNullOrEmpty(s.historySaveError))
                recommendation.text = "HISTORY NOT SAVED\nThe session result is still shown here.\nCheck disk access before your next session.\n\nMOUSE PROTOTYPE — ROM not measured.";
        }
    }
}
