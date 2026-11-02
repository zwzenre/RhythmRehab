using System;

namespace RhythmRehab
{
    public readonly struct GameplayNoteResult
    {
        public readonly int index;
        public readonly HitJudgement judgement;
        public readonly string feedback;
        public readonly bool playSound;
        public GameplayNoteResult(int index, HitJudgement judgement, string feedback, bool playSound = true)
        { this.index = index; this.judgement = judgement; this.feedback = feedback; this.playSound = playSound; }
    }

    // Chart rules and scoring have no references to scene objects or input devices.
    public sealed class GameplayChartRunner
    {
        public ChartNote[] Chart { get; }
        public SessionSummary Summary { get; }
        public float SecondsPerBeat { get; }
        public float ApproachSeconds { get; }
        public float HeightTolerance { get; }
        public float JudgementOffset { get; }
        public double TimingScale { get; }
        public double HitWindow => RhythmRules.GoodWindow * TimingScale;
        public float EndTime { get; }
        public int Combo { get; private set; }
        public int ActiveHoldIndex { get; private set; } = -1;
        public HoldNoteState ActiveHold { get; private set; }
        private readonly bool[] resolved;
        private readonly float heightGrace;
        public event Action<GameplayNoteResult> NoteResolved;
        public event Action<int, HitJudgement> HoldStarted;

        public GameplayChartRunner(ChartNote[] chart, float bpm, float approach, float tolerance, double timingScale,
            float offset, float holdGrace, SessionSummary summary)
        {
            if (chart == null || chart.Length == 0) throw new ArgumentException("A chart needs at least one note.");
            Chart = chart; Summary = summary; SecondsPerBeat = 60f / Math.Max(1, bpm);
            ApproachSeconds = approach; HeightTolerance = tolerance; TimingScale = timingScale;
            JudgementOffset = offset; heightGrace = holdGrace; resolved = new bool[chart.Length];
            summary.totalNotes = chart.Length; summary.holdNotes = 0;
            float end = 0;
            foreach (var note in chart) { end = Math.Max(end, note.EndBeat * SecondsPerBeat); if (note.IsHold) summary.holdNotes++; }
            EndTime = end + .75f;
        }
        public bool IsResolved(int index) => resolved[index];
        public void Advance(double time, float height, bool held)
        {
            UpdateHold(time, height, held);
            for (int i = 0; i < Chart.Length; i++)
                if (!resolved[i] && i != ActiveHoldIndex && time - Chart[i].beat * SecondsPerBeat - JudgementOffset > HitWindow)
                    Resolve(i, HitJudgement.Miss, "Keep going");
        }
        public bool TryHit(double time, float height)
        {
            if (ActiveHoldIndex >= 0) return false;
            int candidate = -1; double closest = HitWindow + .000001;
            for (int i = 0; i < Chart.Length; i++)
            {
                if (resolved[i]) continue;
                double error = Math.Abs(RhythmPreferences.TimingError(time, Chart[i].beat * SecondsPerBeat, JudgementOffset));
                if (error <= closest) { closest = error; candidate = i; }
            }
            if (candidate < 0) return false;
            float heightError = Math.Abs(height - Chart[candidate].TargetHeight);
            Summary.heightAttempts++;
            if (heightError <= HeightTolerance) Summary.correctHeightAttempts++;
            var judgement = RhythmRules.Judge(RhythmPreferences.TimingError(time, Chart[candidate].beat * SecondsPerBeat, JudgementOffset), heightError, HeightTolerance, TimingScale);
            if (Chart[candidate].IsHold && judgement != HitJudgement.Miss)
            {
                ActiveHoldIndex = candidate;
                ActiveHold = new HoldNoteState(Chart[candidate].beat * SecondsPerBeat + JudgementOffset,
                    Chart[candidate].EndBeat * SecondsPerBeat + JudgementOffset, Chart[candidate].TargetHeight, HeightTolerance, judgement, heightGrace);
                HoldStarted?.Invoke(candidate, judgement);
            }
            else Resolve(candidate, judgement, judgement == HitJudgement.Miss ? (heightError > HeightTolerance ? "Adjust your height" : "Keep going") : judgement.ToString());
            return true;
        }
        public HoldOutcome UpdateHold(double time, float height, bool held)
        {
            if (ActiveHoldIndex < 0) return HoldOutcome.Inactive;
            var outcome = ActiveHold.Step(time, height, held);
            if (outcome == HoldOutcome.Completed) Resolve(ActiveHoldIndex, ActiveHold.HeadJudgement, ActiveHold.HeadJudgement + " HOLD", false);
            else if (outcome == HoldOutcome.Broken) Resolve(ActiveHoldIndex, HitJudgement.Miss, held ? "Keep your height steady" : "Hold a little longer");
            return outcome;
        }
        private void Resolve(int index, HitJudgement judgement, string feedback, bool playSound = true)
        {
            if (resolved[index]) return;
            resolved[index] = true;
            if (Chart[index].IsHold) { if (judgement == HitJudgement.Miss) Summary.brokenHolds++; else Summary.completedHolds++; }
            if (index == ActiveHoldIndex) { ActiveHoldIndex = -1; ActiveHold = null; }
            Summary.Record(judgement);
            Combo = judgement == HitJudgement.Miss ? 0 : Combo + 1;
            Summary.bestCombo = Math.Max(Combo, Summary.bestCombo);
            NoteResolved?.Invoke(new GameplayNoteResult(index, judgement, feedback, playSound));
        }
    }
}
