using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RhythmRehab
{
    // Coordinates the session. Note rules, rendering, HUD, clock and audio live in focused classes.
    public sealed class PrototypeGameplay : MonoBehaviour
    {
        [Header("Fallback tempo chart")]
        [SerializeField] private float bpm = 120f;
        [SerializeField] private float approachSeconds = 3f;
        [SerializeField, Range(.03f, .3f)] private float heightTolerance = .14f;
        [SerializeField] private ChartNote[] chart;
        [SerializeField, Min(0)] private float holdHeightGraceSeconds = .12f;
        [Header("Scene responsibilities")]
        [SerializeField] private GameplayPlayfield playfieldView;
        [SerializeField] private GameplayHud hud;
        [SerializeField] private GameplayAudio audioService;
        [Header("Successful-hit effects")]
        [SerializeField] private NoteHitEffects hitEffects;
        [SerializeField] private PlayerMarkerTrail playerMarkerTrail;
        [SerializeField] private GameplayVisualLevels visualLevels;

        private GameplayChartRunner runner;
        private GameplaySessionClock clock;
        private bool finished, continuousHeights;
        private bool starting = true;
        private float currentHeight;
        private double timingScale = 1;
        private AudioClip songAudio;
        private float sessionDuration, audioLeadIn;
        private string selectedMode = "TEMPO TEST", selectedSong = "Tempo Practice";
        private LocalSettings hitControls;

        public float Elapsed => starting ? 0 : clock?.Elapsed ?? 0;
        public bool IsStarting => starting;
        public float CurrentHeight => currentHeight;
        public bool IsPaused => starting || (clock != null && clock.IsPaused);
        public int ActiveHoldIndex => runner?.ActiveHoldIndex ?? -1;
        public bool WaitingForHoldResume => clock != null && clock.WaitingForHoldResume;
        public int JudgedNotes => Summary?.Judged ?? 0;
        public SessionSummary Summary => runner?.Summary;
        public GameplayChartRunner Notes => runner;
        public GameplayPlayfield Playfield => playfieldView;
        public GameplayHud Hud => hud;
        public GameplayAudio Audio => audioService;
        public GameplaySessionClock Clock => clock;
        public bool UsingSensor => UsbSensorConnection.Instance && UsbSensorConnection.Instance.InputEnabled;

        private void Awake() => clock = new GameplaySessionClock(() => AudioSettings.dspTime);
        private IEnumerator Start()
        {
            if (!playfieldView || !hud || !audioService)
            { Debug.LogError("Gameplay needs its Playfield, HUD and Audio components assigned.", this); enabled = false; yield break; }
            playfieldView.HideAllNotes();
            hud.SetPauseMenu(false);
            while (IrisSceneTransition.IsBusy) yield return null;
            ApplySessionSelection();
            var settings = LocalSettingsStore.Default.Load(out var error);
            hitControls = settings;
            if (error != null) Debug.LogWarning("Settings could not be loaded: " + error);
            float travel = RhythmPreferences.TravelSeconds(approachSeconds, settings.noteSpeed);
            if (chart == null || chart.Length == 0 || !playfieldView.CanDisplay(chart, bpm, travel, RhythmRules.GoodWindow * timingScale, settings.judgementOffsetMs / 1000f))
            { Debug.LogError("Gameplay chart needs one authored note view per note.", this); enabled = false; yield break; }

            var sensor = UsbSensorConnection.Instance;
            if (sensor) sensor.PrepareGameplayInput();
            var summary = new SessionSummary { songTitle = selectedSong, modeLabel = selectedMode, bpm = bpm, wellbeing = SessionWellbeing.Snapshot };
            summary.sensorHeightControl = UsingSensor || (sensor && sensor.SensorControlSelected);
            runner = new GameplayChartRunner(chart, bpm, travel,
                heightTolerance, timingScale, settings.judgementOffsetMs / 1000f, holdHeightGraceSeconds, summary, sessionDuration);
            runner.NoteResolved += HandleNoteResult;
            runner.HoldStarted += HandleHoldStart;
            playfieldView.Initialize(runner.Chart);
            hud.Initialize(summary, selectedSong, selectedMode, bpm);
            hud.UpdateTimeline(runner, 0);
            yield return hud.ShowReady();
            if (finished) yield break;
            clock.Start(AudioSettings.dspTime + .2);
            audioService.Initialize(settings, bpm, runner.EndTime + 1, clock.StartDsp, songAudio, audioLeadIn);
            starting = false;
            hud.UpdateTimeline(runner, Elapsed);
        }
        private void ApplySessionSelection()
        {
            if (!SessionSelection.Configured) return;
            var profile = SessionSelection.BuildProfile(SessionSelection.Song, SessionSelection.Mode, SessionSelection.Height, SessionSelection.Difficulty);
            chart = profile.chart; bpm = profile.bpm; approachSeconds = profile.approachSeconds;
            heightTolerance = profile.heightTolerance; timingScale = profile.timingScale;
            sessionDuration = profile.durationSeconds; songAudio = SessionSelection.Song.audio;
            audioLeadIn = SessionSelection.Song.audioLeadInSeconds;
            selectedMode = profile.label; selectedSong = SessionSelection.Song.title;
            continuousHeights = SessionSelection.Mode == SessionMode.Free;
        }
        private void Update()
        {
            if (starting || finished || runner == null || IrisSceneTransition.IsBusy) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) TogglePause();
            var sensor = UsbSensorConnection.Instance;
            if (UsingSensor || (sensor && sensor.SensorControlSelected)) Summary.sensorHeightControl = true;
            if (sensor && sensor.InputEnabled && !sensor.TryGetHeight(out _))
            {
                if (!clock.PauseMenuOpen) TogglePause();
                hud.ShowSensorUnavailable();
                return;
            }
            if (clock.PauseMenuOpen) return;
            if (sensor && sensor.InputEnabled) { if (sensor.TryGetHeight(out var sensorHeight)) currentHeight = sensorHeight; }
            else if (playfieldView.ReadMouseHeight(out var height)) currentHeight = height;
            playfieldView.MoveMarker(currentHeight);
            hud.ShowHeight(currentHeight, continuousHeights, heightTolerance);
            bool held = UsingSensor || HitControls.Held(hitControls);
            if (WaitingForHoldResume)
            {
                hud.ShowResumePrompt();
                if (!ResumeHeldNote(currentHeight, held)) return;
            }
            float time = Elapsed;
            runner.Advance(time, currentHeight, held);
            playfieldView.Render(runner, time);
            if (UsingSensor) runner.TryHit(time, currentHeight, automatic: true);
            else if (HitControls.Pressed(hitControls)) TryHit(time, currentHeight);
            hud.UpdateTimeline(runner, time);
            if (time >= runner.EndTime) CompleteSession();
        }
        public bool TryHit(double songTime, float height) => !finished && !IsPaused && runner != null && runner.TryHit(songTime, height);
        public HoldOutcome UpdateActiveHold(double songTime, float height, bool held)
        {
            if (ActiveHoldIndex < 0) return HoldOutcome.Inactive;
            return finished || IsPaused ? HoldOutcome.Holding : runner.UpdateHold(songTime, height, held);
        }
        public bool ResumeHeldNote(float height, bool held)
        {
            if (!WaitingForHoldResume || !held || runner.ActiveHold == null || Mathf.Abs(height - runner.ActiveHold.TargetHeight) > heightTolerance) return false;
            clock.ResumeHold(); audioService.SetPaused(false); if (hitEffects) hitEffects.SetPaused(false); return true;
        }
        private void HandleHoldStart(int index, HitJudgement judgement)
        {
            float height = runner.Chart[index].TargetHeight;
            hud.ShowFeedback("HOLD", playfieldView.HeightColor(height), judgement);
            audioService.PlayHit();
            if (playerMarkerTrail) playerMarkerTrail.PulseOutline();
            if (hitEffects) hitEffects.Play(playfieldView.PositionAt(height), judgement);
        }
        private void HandleNoteResult(GameplayNoteResult result)
        {
            playfieldView.HideNote(result.index);
            var colour = result.judgement == HitJudgement.Miss ? playfieldView.MissColour : hitEffects ? hitEffects.ColorFor(result.judgement) : new Color(.5f, 1, .8f);
            hud.ShowFeedback(result.feedback, colour, result.judgement);
            if (result.judgement != HitJudgement.Miss)
            {
                var position = playfieldView.PositionAt(runner.Chart[result.index].TargetHeight);
                if (result.playSound)
                {
                    audioService.PlayHit();
                    if (playerMarkerTrail) playerMarkerTrail.PulseOutline();
                }
                if (hitEffects) hitEffects.Play(position, result.judgement);
                hud.PunchScore();
                if (visualLevels) visualLevels.OnSuccessfulHit(position, result.judgement, colour);
            }
            hud.RefreshScore(Summary, runner.Combo);
        }
        public void TogglePause()
        {
            if (starting || finished || runner == null) return;
            if (hud.ClosePauseSettings()) return;
            if (hud.BackFromEndOptions()) return;
            var sensor = UsbSensorConnection.Instance;
            if (clock.PauseMenuOpen && sensor && (sensor.IsCapturing || (UsingSensor && !sensor.HasFreshReading))) return;
            clock.TogglePause(ActiveHoldIndex >= 0);
            hud.SetPauseMenu(clock.PauseMenuOpen); audioService.SetPaused(IsPaused);
            if (hitEffects) hitEffects.SetPaused(IsPaused);
        }
        public void OpenPauseSettings()
        {
            if (clock.PauseMenuOpen) hud.OpenPauseSettings();
        }

        public bool SelectControls(bool sensorControl)
        {
            if (!clock.PauseMenuOpen) return false;
            var sensor = UsbSensorConnection.Instance;
            if (sensorControl)
            {
                if (!sensor || !sensor.HasFreshReading || !sensor.Calibration.IsReady) return false;
                sensor.UseSensor();
                Summary.sensorHeightControl = true;
            }
            else if (sensor) sensor.UseMouse();
            return true;
        }
        public void Restart() => LeaveScene("Gameplay");
        public void Home() => LeaveScene("MainMenu");
        public void ReviewEndSession()
        {
            if (clock.PauseMenuOpen && !finished) hud.ShowEndOptions();
        }
        public void DiscardSession()
        {
            if (clock.PauseMenuOpen && !finished && hud.IsDiscardConfirmationOpen) LeaveScene("MainMenu");
        }
        private void LeaveScene(string scene)
        {
            bool removal = UsingSensor || Summary?.sensorHeightControl == true ||
                (UsbSensorConnection.Instance && UsbSensorConnection.Instance.SensorControlSelected);
            if (!(removal ? SensorRemovalPresenter.ShowBefore(scene) : IrisSceneTransition.Load(scene))) return;
            finished = true; audioService.Stop();
        }
        public void CompleteSession()
        {
            if (starting || finished || runner == null || IrisSceneTransition.IsBusy) return;
            var summary = Summary;
            summary.duration = Mathf.Max(0, Mathf.Min(Elapsed, runner.EndTime));
            summary.endedUtc = DateTime.UtcNow.ToString("O");
            summary.endedEarly = Elapsed < runner.EndTime;
            summary.sensorHeightControl |= UsingSensor || (UsbSensorConnection.Instance && UsbSensorConnection.Instance.SensorControlSelected);
            if (!SessionHistoryStore.Default.TrySave(summary, out var error))
            { summary.historySaveError = error; Debug.LogWarning("Session history could not be saved: " + error); }
            SessionSummary.Last = summary; finished = true; audioService.Stop();
            if (summary.sensorHeightControl) SensorRemovalPresenter.ShowBefore("Results");
            else IrisSceneTransition.Load("Results");
        }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused && !starting && runner != null && !clock.PauseMenuOpen && !finished) TogglePause();
        }
        private void OnDestroy()
        {
            if (runner == null) return;
            runner.NoteResolved -= HandleNoteResult; runner.HoldStarted -= HandleHoldStart;
        }
    }
}
