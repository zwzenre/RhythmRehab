using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RhythmRehab;
public static class HoldNotesProbe
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static string status;static int checks,index;static double start,end,frozen;static float began;
    static PrototypeGameplay game;static ChartNote[] chart;static bool[] resolved;static NoteHitEffects effects;static int finalBefore;
    static object Field(object target,string name)=>GameplayEditorAccess.Read(target,name);
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);checks++;}
    static void Reset(){resolved[index]=false;}
    static bool Begin(double error=0)=>game.TryHit(start+error,chart[index].TargetHeight);
    public static string Main()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play first.");
        game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(!game||game.Summary==null||IrisSceneTransition.IsBusy)throw new Exception("Wait for Gameplay.");
        game.enabled=false;if(game.IsPaused)game.TogglePause();GameplayEditorAccess.Write(game,"finished",false);
        checks=0;status="RUNNING";
        try
        {
            chart=(ChartNote[])Field(game,"chart");resolved=(bool[])Field(game,"resolved");index=Array.FindIndex(chart,n=>n.IsHold);
            Check(index>=0,"Demo chart contains hold notes");Array.Clear(resolved,0,resolved.Length);
            start=chart[index].beat*60.0/game.Summary.bpm+(float)Field(game,"judgementOffsetSeconds");end=chart[index].EndBeat*60.0/game.Summary.bpm+(float)Field(game,"judgementOffsetSeconds");
            effects=(NoteHitEffects)Field(game,"hitEffects");var pulse=(ScorePulse)Field(game,"scorePulse");
            var views=(HoldNoteView[])Field(game,"holdViews");
            Check(views.Length==((RectTransform[])Field(game,"noteViews")).Length&&views.All(v=>v),"Every note slot has an authored hold view");
            Check(views.All(v=>v.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g=>!g.raycastTarget)),"Hold decorations do not block input");
            int score=game.Summary.score,judged=game.JudgedNotes,punches=pulse.PulseCount;
            Check(Begin()&&game.ActiveHoldIndex==index&&!resolved[index],"Correct head starts a hold without resolving it");
            Check(game.Summary.score==score&&game.JudgedNotes==judged&&pulse.PulseCount==punches,"Hold head does not award score or combo early");
            Check(!Begin(),"Repeated presses cannot consume the same hold again");
            Check(game.UpdateActiveHold((start+end)*.5,chart[index].TargetHeight,true)==HoldOutcome.Holding,"Maintaining key and height keeps hold active");
            var body=(UnityEngine.UI.Image)Field(views[index],"body");var caption=(TMPro.TMP_Text)Field(views[index],"caption");
            views[index].Render(400,Color.green,true,.25f);float width=body.rectTransform.rect.width;
            views[index].Render(200,Color.green,true,.5f);
            Check(body.rectTransform.rect.width<width&&caption.text=="HOLD 50%","Hold body shrinks and caption shows progress");
            Check(game.UpdateActiveHold(end,chart[index].TargetHeight,false)==HoldOutcome.Completed,"Release at tail completes hold");
            Check(game.ActiveHoldIndex<0&&resolved[index]&&game.Summary.score==score+1000&&game.JudgedNotes==judged+1,"Completed hold scores exactly one Perfect note");
            Check(pulse.PulseCount==punches+1&&game.Summary.completedHolds>=1,"Hold completion pulses score and records hold stats");
            score=game.Summary.score;Check(game.UpdateActiveHold(end+1,0,false)==HoldOutcome.Inactive&&game.Summary.score==score,"Repeated completion cannot award extra points");
            Reset();Check(Begin(),"Second hold can start");int bursts=effects.PlayedBursts,miss=game.Summary.miss;
            Check(game.UpdateActiveHold(end-.2,chart[index].TargetHeight,false)==HoldOutcome.Broken,"Early release breaks hold");
            Check(game.Summary.miss==miss+1&&effects.PlayedBursts==bursts&&game.Summary.score==score,"Broken hold gives one Miss with no new hit effect or score");
            Reset();Begin();float wrong=chart[index].TargetHeight>.5f?0:1;
            Check(game.UpdateActiveHold(start+.1,wrong,true)==HoldOutcome.Holding,"Short height slip is tolerated");
            Check(game.UpdateActiveHold(start+.25,wrong,true)==HoldOutcome.Broken,"Sustained height error breaks hold");
            Reset();Begin();game.UpdateActiveHold(start+.1,wrong,true);game.UpdateActiveHold(start+.15,chart[index].TargetHeight,true);
            Check(game.UpdateActiveHold(end,chart[index].TargetHeight,true)==HoldOutcome.Completed,"Recovering a small height slip allows completion");
            foreach(var pair in new[]{new[]{.10,700.0},new[]{.18,400.0}})
            {
                Reset();score=game.Summary.score;double scale=(double)Field(game,"timingScale");Begin(pair[0]*scale);
                game.UpdateActiveHold(end,chart[index].TargetHeight,true);
                Check(game.Summary.score==score+(int)pair[1],"Great/Good head timing is preserved through completion");
            }
            Reset();bursts=effects.PlayedBursts;miss=game.Summary.miss;
            Check(game.TryHit(start,wrong)&&game.ActiveHoldIndex<0&&game.Summary.miss==miss+1&&effects.PlayedBursts==bursts,"Wrong-height head misses immediately without effects");
            var last=chart[chart.Length-1];var changed=last;changed.holdBeats=4;chart[chart.Length-1]=changed;
            float chartEnd=new GameplayChartRunner(chart,game.Summary.bpm,game.Notes.ApproachSeconds,game.Notes.HeightTolerance,game.Notes.TimingScale,game.Notes.JudgementOffset,.12f,new SessionSummary()).EndTime;
            Check(chartEnd>=changed.EndBeat*60/game.Summary.bpm+.749f,"Countdown includes final hold tail");chart[chart.Length-1]=last;
            Reset();Begin();game.UpdateActiveHold(start+.25,chart[index].TargetHeight,true);game.TogglePause();
            Check(game.IsPaused&&game.UpdateActiveHold(end+5,wrong,false)==HoldOutcome.Holding,"Pause cannot break or complete an active hold");
            game.TogglePause();Check(game.WaitingForHoldResume&&game.IsPaused,"Resume waits for key and target height");
            Check(!game.ResumeHeldNote(chart[index].TargetHeight,false)&&!game.ResumeHeldNote(wrong,true),"Resume rejects released key and wrong height");
            frozen=game.Elapsed;began=Time.realtimeSinceStartup;finalBefore=game.Summary.completedHolds;
            EditorApplication.update+=Tick;return "Checking hold success, release, height, scoring, progress, final duration and pause/resume.";
        }
        catch(Exception error){status="FAIL "+error.Message;return status;}
    }
    static void Tick()
    {
        try
        {
            if(Time.realtimeSinceStartup-began<.25f)return;
            Check(Math.Abs(game.Elapsed-frozen)<.005,"Waiting for hold resume freezes chart time");
            game.TogglePause();game.TogglePause();Check(Math.Abs(game.Elapsed-frozen)<.03,"Reopening pause while waiting does not jump chart time");
            Check(game.ResumeHeldNote(chart[index].TargetHeight,true)&&!game.IsPaused,"Holding key at target resumes correctly");
            Check(game.UpdateActiveHold(end,chart[index].TargetHeight,true)==HoldOutcome.Completed&&game.Summary.completedHolds==finalBefore+1,"Hold can complete after pause and resume");
            var marker=(RectTransform)Field(game,"playerMarker");Check(marker.GetComponent<UnityEngine.UI.Image>().color==Color.white,"Mouse marker remains white");
            var snapshot=JsonUtility.FromJson<SessionSummary>(JsonUtility.ToJson(game.Summary));
            Check(snapshot.completedHolds==game.Summary.completedHolds&&snapshot.brokenHolds==game.Summary.brokenHolds,"Live hold stats survive session serialization");
            status="PASS "+checks+" live hold checks. No sessions or settings saved.";EditorApplication.update-=Tick;
        }
        catch(Exception error){status="FAIL "+error.Message;EditorApplication.update-=Tick;}
    }
    public static string Status()=>status;
}
