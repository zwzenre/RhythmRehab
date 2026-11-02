using System;

namespace RhythmRehab
{
    public sealed class GameplaySessionClock
    {
        private readonly Func<double> now;
        private double startDsp, pausedDsp;
        public bool PauseMenuOpen { get; private set; }
        public bool WaitingForHoldResume { get; private set; }
        public bool IsPaused => PauseMenuOpen || WaitingForHoldResume;
        public double StartDsp => startDsp;
        public float Elapsed => (float)((IsPaused ? pausedDsp : now()) - startDsp);

        public GameplaySessionClock(Func<double> readDspTime) { now = readDspTime; }
        public void Start(double scheduledDspTime) { startDsp = scheduledDspTime; PauseMenuOpen = WaitingForHoldResume = false; }
        public void TogglePause(bool holdingNote)
        {
            if (!PauseMenuOpen)
            {
                if (WaitingForHoldResume) startDsp += now() - pausedDsp;
                WaitingForHoldResume = false;
                pausedDsp = now();
                PauseMenuOpen = true;
            }
            else
            {
                startDsp += now() - pausedDsp;
                PauseMenuOpen = false;
                WaitingForHoldResume = holdingNote;
                if (holdingNote) pausedDsp = now();
            }
        }
        public void ResumeHold()
        {
            if (!WaitingForHoldResume) return;
            startDsp += now() - pausedDsp;
            WaitingForHoldResume = false;
        }
    }
}
