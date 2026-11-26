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
        [Header("Pause recovery")]
        [SerializeField, Min(.25f)] private float resumeDelaySeconds = 3f;
        [Header("Successful-hit effects")]
        [SerializeField] private NoteHitEffects hitEffects;
        [SerializeField] private PlayerMarkerTrail playerMarkerTrail;
        [SerializeField] private GameplayVisualLevels visualLevels;
        [SerializeField] private GameplayAtmosphere atmosphere;
        [SerializeField] private ChartFinishCelebration finishCelebration;

        private GameplayChartRunner runner;
        private SessionSummary sessionSummary;
        private GameplaySessionClock clock;
        private bool finished, continuousHeights;
        private bool starting = true;
        private float currentHeight;
        private double timingScale = 1;
        private AudioClip songAudio;
        private float sessionDuration, audioLeadIn;
        private string selectedMode = "TEMPO TEST", selectedSong = "Tempo Practice";
        private LocalSettings hitControls;
        private bool resuming;
        private Coroutine resumeRoutine;
        private RehabilitationPlan plan;
        private LegRaiseTracker legRaises;

        public float Elapsed => starting ? 0 : clock?.Elapsed ?? 0;
        public bool IsStarting => starting;
        public bool IsResuming => resuming;
        public float CurrentHeight => currentHeight;
        public bool IsPaused => starting || (clock != null && clock.IsPaused);
        public int ActiveHoldIndex => runner?.ActiveHoldIndex ?? -1;
        public bool WaitingForHoldResume => clock != null && clock.WaitingForHoldResume;
        public int JudgedNotes => Summary?.Judged ?? 0;
        public SessionSummary Summary => sessionSummary;
        public GameplayChartRunner Notes => runner;
        public GameplayPlayfield Playfield => playfieldView;
        public GameplayHud Hud => hud;
        public GameplayAudio Audio => audioService;
        public GameplaySessionClock Clock => clock;
        public bool UsingSensor => UsbSensorConnection.Instance && UsbSensorConnection.Instance.InputEnabled;
        private bool SensorParticipating => UsingSensor || (plan == null &&
            UsbSensorConnection.Instance && UsbSensorConnection.Instance.SensorControlSelected);

        private void Awake() => clock = new GameplaySessionClock(() => AudioSettings.dspTime);
        private void LateUpdate()
        {
            // Keep absolute mouse height input; only hide the pointer itself.
            Cursor.visible = finished || !Application.isFocused ||
                (clock != null && clock.PauseMenuOpen && !resuming);
        }
        private void OnDisable() => Cursor.visible = true;
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
            float targetDegrees = plan?.TargetDegrees(SessionSelection.Height) ?? 0;
            if (sensor) sensor.PrepareGameplayInput(Mathf.Max(0, targetDegrees - .5f));
            var summary = new SessionSummary { songTitle = selectedSong, modeLabel = selectedMode, bpm = bpm, wellbeing = SessionWellbeing.Snapshot };
            if (SessionSelection.Configured && SessionSelection.Mode == SessionMode.Free)
            {
                summary.freeMode = true;
                summary.songId = SessionSelection.Song.id;
                summary.freeDifficulty = SessionSelection.Difficulty;
            }
            sessionSummary = summary;
            if (hitEffects && summary.wellbeing != null && !summary.wellbeing.skipped)
                hitEffects.SetReducedFeedback(summary.wellbeing.highPain || summary.wellbeing.highStress);
            summary.patientId = PatientContext.PatientId;
            summary.patientName = PatientContext.DisplayName;
            summary.plan = plan;
            if (plan != null)
            {
                summary.targetShinTiltDegrees = targetDegrees;
                legRaises = new LegRaiseTracker(plan, targetDegrees);
            }
            summary.sensorHeightControl = SensorParticipating;
            summary.mousePractice = plan != null && !UsingSensor;
            runner = new GameplayChartRunner(chart, bpm, travel, heightTolerance, timingScale,
                settings.judgementOffsetMs / 1000f, holdHeightGraceSeconds, summary, sessionDuration);
            runner.NoteResolved += HandleNoteResult;
            runner.HoldStarted += HandleHoldStart;
            playfieldView.Initialize(chart);
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
            if (SessionSelection.Mode == SessionMode.Therapy && SessionSelection.Plan?.patientId == PatientContext.PatientId)
                plan = SessionSelection.Plan;
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
            if (starting || finished || Summary == null || IrisSceneTransition.IsBusy) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) TogglePause();
            var sensor = UsbSensorConnection.Instance;
            bool sensorControl = UsingSensor;
            Summary.sensorHeightControl |= SensorParticipating;
            float sensorHeight = 0;
            if (sensorControl && !sensor.TryGetHeight(out sensorHeight))
            {
                ResetMovementAttempt();
                if (resuming) CancelResume();
                if (!clock.PauseMenuOpen) TogglePause();
                hud.ShowSensorUnavailable();
                return;
            }
            if (clock.PauseMenuOpen) { ResetMovementAttempt(); return; }
            if (Elapsed >= 0 && Summary.freeMode)
            {
                Summary.playedWithSensor |= sensorControl;
                Summary.playedWithKeyboard |= !sensorControl;
            }
            float degrees = 0;
            if (sensorControl)
            {
                currentHeight = sensorHeight;
                if (plan != null)
                {
                    degrees = Mathf.Round(sensor.MovementDegrees * 10f) / 10f;
                    currentHeight = plan.GameplayHeight(degrees);
                }
            }
            else if (playfieldView.ReadMouseHeight(out var height)) currentHeight = height;

            if (plan != null)
            {
                if (!sensorControl)
                {
                    Summary.mousePractice = true;
                    degrees = plan.PracticeDegrees(currentHeight);
                }
                if (Elapsed >= 0) legRaises.Record(degrees, Summary);
            }
            playfieldView.MoveMarker(currentHeight);
            if (plan != null) hud.ShowPlanHeight(degrees, Summary, !sensorControl);
            else hud.ShowHeight(currentHeight, continuousHeights, heightTolerance);
            bool held = UsingSensor || HitControls.Held(hitControls);
            if (WaitingForHoldResume)
            {
                hud.ShowResumePrompt();
                if (!ResumeHeldNote(currentHeight, held)) return;
            }
            float time = Elapsed;
            float judgedHeight = currentHeight;
            if (plan != null && sensorControl && (currentHeight < ChartNote.LowTargetHeight - .0001f || currentHeight > 1.0001f))
                judgedHeight = -1;
            runner.Advance(time, judgedHeight, held);
            playfieldView.Render(runner, time);
            if (UsingSensor) runner.TryHit(time, judgedHeight, automatic: true);
            else if (HitControls.Pressed(hitControls)) TryHit(time, currentHeight);
            hud.UpdateTimeline(runner, time);
            if (time >= runner.EndTime) CompleteSession();
        }
        private void ResetMovementAttempt() => legRaises?.Reset();
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
            if (atmosphere) atmosphere.PulseLane(playfieldView.PositionAt(height), playfieldView.HeightColor(height));
        }
        private void HandleNoteResult(GameplayNoteResult result)
        {
            if (runner.Chart[result.index].IsHold && result.judgement != HitJudgement.Miss)
                playfieldView.ShowHoldCompletion(result.index, Elapsed);
            else
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
                if (atmosphere) atmosphere.PulseLane(position, playfieldView.HeightColor(runner.Chart[result.index].TargetHeight));
                hud.PunchScore();
                if (visualLevels) visualLevels.OnSuccessfulHit(position, result.judgement, colour);
            }
            hud.RefreshScore(Summary, runner.Combo);
        }
        public void TogglePause()
        {
            if (starting || finished || Summary == null || IrisSceneTransition.IsBusy) return;
            if (resuming) { CancelResume(); return; }
            if (hud.ClosePauseSettings()) return;
            if (hud.BackFromEndOptions()) return;
            var sensor = UsbSensorConnection.Instance;
            if (clock.PauseMenuOpen && UsingSensor && (sensor.IsCapturing || !sensor.HasFreshReading)) return;
            if (clock.PauseMenuOpen)
            {
                resuming = true;
                hud.SetPauseMenu(false);
                resumeRoutine = StartCoroutine(ResumeAfterDelay());
                return;
            }
            clock.TogglePause(ActiveHoldIndex >= 0);
            hud.SetPauseMenu(clock.PauseMenuOpen); audioService.SetPaused(IsPaused);
            if (hitEffects) hitEffects.SetPaused(IsPaused);
        }
        private IEnumerator ResumeAfterDelay()
        {
            // Leave the session clock and audio paused throughout the ready overlay.
            yield return hud.ShowReady(resumeDelaySeconds);
            resumeRoutine = null;
            if (finished) { resuming = false; yield break; }
            var sensor = UsbSensorConnection.Instance;
            if (UsingSensor && (sensor.IsCapturing || !sensor.HasFreshReading))
            { CancelResume(); hud.ShowSensorUnavailable(); yield break; }
            resuming = false;
            clock.TogglePause(ActiveHoldIndex >= 0);
            audioService.SetPaused(IsPaused);
            if (hitEffects) hitEffects.SetPaused(IsPaused);
        }
        private void CancelResume()
        {
            if (resumeRoutine != null) StopCoroutine(resumeRoutine);
            resumeRoutine = null; resuming = false;
            hud.HideReady(); hud.SetPauseMenu(true);
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
                if (plan != null && sensor.Calibration.RangeDegrees + .5f < Summary.targetShinTiltDegrees) return false;
                sensor.UseSensor();
                Summary.sensorHeightControl = true;
            }
            else
            {
                if (sensor) sensor.UseMouse();
                if (plan != null) Summary.mousePractice = true;
            }
            ResetMovementAttempt();
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
            bool removal = scene != "Gameplay" && (SensorParticipating || Summary?.sensorHeightControl == true);
            if (!(removal ? SensorRemovalPresenter.ShowBefore(scene) : IrisSceneTransition.Load(scene))) return;
            finished = true; audioService.Stop();
        }
        public void CompleteSession()
        {
            if (starting || finished || Summary == null || IrisSceneTransition.IsBusy) return;
            var summary = Summary;
            summary.duration = Mathf.Max(0, Mathf.Min(Elapsed, runner.EndTime));
            summary.endedUtc = DateTime.UtcNow.ToString("O");
            summary.endedEarly = Elapsed < runner.EndTime;
            summary.sensorHeightControl |= SensorParticipating;
            if (!SessionHistoryStore.Default.TrySave(summary, out var error))
            { summary.historySaveError = error; Debug.LogWarning("Session history could not be saved: " + error); }
            SessionSummary.Last = summary; finished = true; audioService.Stop();
            var award = ChartFinishCelebration.AwardFor(summary);
            if (finishCelebration && award != ChartAward.None)
                StartCoroutine(CelebrateAndOpenResults(award, summary.sensorHeightControl));
            else
                OpenResults(summary.sensorHeightControl);
        }

        private IEnumerator CelebrateAndOpenResults(ChartAward award, bool sensorUsed)
        {
            yield return finishCelebration.Play(award);
            OpenResults(sensorUsed);
        }

        private static void OpenResults(bool sensorUsed)
        {
            if (sensorUsed) SensorRemovalPresenter.ShowBefore("Results");
            else IrisSceneTransition.Load("Results");
        }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused && resuming) { CancelResume(); return; }
            if (!focused && !starting && Summary != null && !clock.PauseMenuOpen && !finished) TogglePause();
        }
        private void OnDestroy()
        {
            if (runner == null) return;
            runner.NoteResolved -= HandleNoteResult; runner.HoldStarted -= HandleHoldStart;
        }
    }
}
