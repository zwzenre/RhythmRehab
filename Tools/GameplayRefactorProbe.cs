using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RhythmRehab;
public static class GameplayRefactorProbe
{
    static string status;static int checks,hold;static float began,frozen;static PrototypeGameplay game;
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Field(object target,string name)=>GameplayEditorAccess.Read(target,name);
    static void Check(bool pass,string label){if(!pass)throw new Exception(label);checks++;}
    static double Head(int index)=>game.Notes.Chart[index].beat*game.Notes.SecondsPerBeat+game.Notes.JudgementOffset;
    static double Tail(int index)=>game.Notes.Chart[index].EndBeat*game.Notes.SecondsPerBeat+game.Notes.JudgementOffset;
    public static string Main()
    {
        game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(!game||game.Notes==null||IrisSceneTransition.IsBusy)throw new Exception("Wait for Gameplay.");
        game.enabled=false;if(game.IsPaused)game.TogglePause();checks=0;status="RUNNING";
        try
        {
            Check(game.Playfield&&game.Hud&&game.Audio,"Coordinator has all three scene components");
            Check(game.Playfield.Capacity>=game.Notes.Chart.Length,"All note slots preserved");
            var source=(AudioSource)Field(game.Audio,"hitSource");Check(source.clip.name=="mixkit-short-slap-or-clap-2167","Existing hit clip preserved");
            var effects=(NoteHitEffects)Field(game,"hitEffects");var pulse=(ScorePulse)Field(game.Hud,"scorePulse");
            int score=game.Summary.score;Check(game.TryHit(Head(0),game.Notes.Chart[0].TargetHeight)&&game.Summary.score==score+1000,"Tap scoring still works through coordinator");
            Check(((TMPro.TMP_Text)Field(game.Hud,"scoreLabel")).text==game.Summary.score.ToString("N0"),"HUD reflects scored tap");
            var holds=Enumerable.Range(0,game.Notes.Chart.Length).Where(i=>game.Notes.Chart[i].IsHold).ToArray();Check(holds.Length>=5,"Existing demo holds retained");
            hold=holds[0];score=game.Summary.score;int punches=pulse.PulseCount;
            Check(game.TryHit(Head(hold),game.Notes.Chart[hold].TargetHeight)&&game.Summary.score==score,"Hold starts without premature score");
            Check(game.UpdateActiveHold(Tail(hold),game.Notes.Chart[hold].TargetHeight,false)==HoldOutcome.Completed&&game.Summary.score==score+1000,"Hold completes once");
            Check(pulse.PulseCount==punches+1,"Completion score animation preserved");
            hold=holds[1];game.TryHit(Head(hold),game.Notes.Chart[hold].TargetHeight);int bursts=effects.PlayedBursts,miss=game.Summary.miss;
            Check(game.UpdateActiveHold(Tail(hold)-.2,game.Notes.Chart[hold].TargetHeight,false)==HoldOutcome.Broken,"Early release still fails");
            Check(game.Summary.miss==miss+1&&effects.PlayedBursts==bursts,"Miss records once with no new particles");
            hold=holds[2];game.TryHit(Head(hold),game.Notes.Chart[hold].TargetHeight);float wrong=game.Notes.Chart[hold].TargetHeight>.5f?0:1;
            game.UpdateActiveHold(Head(hold)+.1,wrong,true);Check(game.UpdateActiveHold(Head(hold)+.25,wrong,true)==HoldOutcome.Broken,"Height failure preserved");
            hold=holds[4];score=game.Summary.score;game.TryHit(Head(hold)+.18*game.Notes.TimingScale,game.Notes.Chart[hold].TargetHeight);game.UpdateActiveHold(Tail(hold),game.Notes.Chart[hold].TargetHeight,true);
            Check(game.Summary.score==score+400,"Good timing survives complete hold");
            hold=holds[3];game.TryHit(Head(hold),game.Notes.Chart[hold].TargetHeight);game.TogglePause();
            Check(game.IsPaused&&game.UpdateActiveHold(Tail(hold)+10,0,false)==HoldOutcome.Holding,"Pause freezes hold rules");
            game.TogglePause();Check(game.WaitingForHoldResume&&game.IsPaused,"Resume still waits for hold input");
            Check(!game.ResumeHeldNote(game.Notes.Chart[hold].TargetHeight,false),"Resume needs held input");
            frozen=game.Elapsed;began=Time.realtimeSinceStartup;EditorApplication.update+=Tick;return "Checking migrated scene references, taps, holds, HUD, particles, audio and pause.";
        }
        catch(Exception error){status="FAIL "+error.Message;return status;}
    }
    static void Tick()
    {
        try
        {
            if(Time.realtimeSinceStartup-began<.25f)return;
            Check(Math.Abs(game.Elapsed-frozen)<.005,"Waiting freezes elapsed time");
            Check(game.ResumeHeldNote(game.Notes.Chart[hold].TargetHeight,true)&&!game.IsPaused,"Correct input resumes hold");
            Check(game.UpdateActiveHold(Tail(hold),game.Notes.Chart[hold].TargetHeight,true)==HoldOutcome.Completed,"Hold completes after pause");
            game.Playfield.MoveMarker(.5f);Check(game.Playfield.Marker.GetComponent<UnityEngine.UI.Image>().color==Color.white,"White mouse marker preserved");
            game.Hud.UpdateTimeline(game.Notes,game.Notes.EndTime-12);
            Check(((TMPro.TMP_Text)Field(game.Hud,"remainingTimeLabel")).text=="00:12","Countdown still shows time only");
            var copy=JsonUtility.FromJson<SessionSummary>(JsonUtility.ToJson(game.Summary));Check(copy.completedHolds==game.Summary.completedHolds&&copy.score==game.Summary.score,"Session data serialization preserved");
            status="PASS "+checks+" live gameplay-refactor checks. No sessions saved.";EditorApplication.update-=Tick;
        }
        catch(Exception error){status="FAIL "+error.Message;EditorApplication.update-=Tick;}
    }
    public static string Status()=>status;
}
