using System;
using System.Linq;
using UnityEngine;
using RhythmRehab;
public static class VerifyHoldNotes
{
    public static string Main()
    {
        int checks=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);checks++;};
        Func<HoldNoteState> make=()=>new HoldNoteState(1,2,.5f,.14f,HitJudgement.Perfect);
        var hold=make();check(hold.Progress(0)==0&&hold.Progress(3)==1,"Progress clamps to valid range");
        check(Math.Abs(hold.Progress(1.5)-.5)<.0001,"Halfway progress");
        check(hold.Step(1.2,.5f,true)==HoldOutcome.Holding,"Held at correct height");
        check(hold.Step(2,.5f,false)==HoldOutcome.Completed,"Release at the exact tail is accepted");
        check(hold.Step(3,0,false)==HoldOutcome.Completed,"Completed state remains completed");
        hold=make();check(hold.Step(1.3,.5f,false)==HoldOutcome.Broken,"Early release breaks hold");
        check(hold.Step(2,.5f,true)==HoldOutcome.Broken,"Repress cannot revive a broken hold");
        hold=make();check(hold.Step(1.2,.9f,true)==HoldOutcome.Holding,"Brief height jitter has grace");
        check(hold.Step(1.25,.5f,true)==HoldOutcome.Holding&&hold.Step(2,.5f,true)==HoldOutcome.Completed,"Recovering height within grace succeeds");
        hold=make();hold.Step(1.2,.9f,true);check(hold.Step(1.34,.9f,true)==HoldOutcome.Broken,"Sustained wrong height breaks hold");
        hold=make();hold.Step(1.8,.9f,true);check(hold.Step(2,.9f,true)==HoldOutcome.Broken,"Tail cannot hide a sustained height error");
        hold=make();check(hold.Step(1.2,.64f,true)==HoldOutcome.Holding,"Height tolerance boundary accepted");
        var late=new HoldNoteState(1.2,2.2,.25f,.1f,HitJudgement.Great);
        check(late.Step(2.19f,.25f,true)==HoldOutcome.Holding&&late.Step(2.2,.25f,true)==HoldOutcome.Completed,"Offset-adjusted end time preserved");
        check(late.HeadJudgement==HitJudgement.Great,"Head timing judgement preserved");
        bool invalid=false;try{new HoldNoteState(1,1,0,.1f,HitJudgement.Perfect);}catch(ArgumentException){invalid=true;}check(invalid,"Invalid duration rejected");
        var song=new SongEntry{id="hold-check",title="Hold check",bpm=120};
        foreach(SessionMode mode in Enum.GetValues(typeof(SessionMode)))foreach(FreeDifficulty difficulty in Enum.GetValues(typeof(FreeDifficulty)))
        {
            var profile=SessionSelection.BuildProfile(song,mode,HeightLevel.Medium,difficulty);
            check(profile.chart.Any(n=>n.IsHold)&&profile.chart.Any(n=>!n.IsHold),"Every mode has both taps and holds");
            check(!profile.chart[0].IsHold&&!profile.chart[1].IsHold,"Opening taps remain available");
            for(int i=0;i<profile.chart.Length-1;i++)if(profile.chart[i].IsHold)
                check(profile.chart[i+1].beat-profile.chart[i].EndBeat>=1,"Recovery gap after each hold");
            if(mode==SessionMode.Therapy)check(profile.chart.All(n=>n.TargetHeight==.5f),"Therapy holds use selected fixed height");
        }
        check(!new ChartNote{beat=2}.IsHold&&new ChartNote{beat=2}.EndBeat==2,"Old chart data stays a tap");
        var json=JsonUtility.ToJson(new SessionSummary{holdNotes=3,completedHolds=2,brokenHolds=1});
        var saved=JsonUtility.FromJson<SessionSummary>(json);check(saved.holdNotes==3&&saved.completedHolds==2&&saved.brokenHolds==1,"Hold stats survive JSON round-trip");
        var legacy=JsonUtility.FromJson<SessionSummary>("{\"totalNotes\":10,\"score\":1000}");
        check(legacy.holdNotes==0&&legacy.completedHolds==0&&legacy.score==1000,"Existing saved sessions remain readable");
        return checks+" hold timing, release, height, chart gap and JSON checks passed.";
    }
}
