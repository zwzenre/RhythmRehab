using System;
using System.Linq;
using RhythmRehab;
public static class VerifyGameplayChartRunner
{
    static GameplayChartRunner Runner(float offset=0)
    {
        var notes=new[]{new ChartNote{beat=2,lane=0},new ChartNote{beat=5,lane=1,holdBeats=2},new ChartNote{beat=10,lane=2}};
        return new GameplayChartRunner(notes,120,3,.14f,1,offset,.12f,new SessionSummary{bpm=120});
    }
    public static string Main()
    {
        int checks=0;Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);checks++;};
        foreach(var sample in new[]{new[]{0.0,1000.0},new[]{.1,700.0},new[]{.18,400.0}})
        {
            var r=Runner();int events=0;r.NoteResolved+=_=>events++;
            check(r.TryHit(1+sample[0],0),"Tap consumed");check(r.Summary.score==(int)sample[1]&&events==1,"Tap timing and event preserved");
            check(!r.TryHit(1,0)&&events==1,"Tap cannot be scored twice");
        }
        var runner=Runner();int resolved=0,started=0;runner.NoteResolved+=_=>resolved++;runner.HoldStarted+=(_,__)=>started++;
        check(runner.TryHit(2.5,.5f)&&started==1&&runner.ActiveHoldIndex==1,"Hold head starts once");
        check(resolved==0&&runner.Summary.score==0&&runner.Combo==0,"No premature hold judgement");
        check(!runner.TryHit(2.5,.5f),"Held note cannot restart");
        check(runner.UpdateHold(3,.5f,true)==HoldOutcome.Holding,"Hold maintains height");
        check(runner.UpdateHold(3.5,.5f,false)==HoldOutcome.Completed,"Tail completion accepts release");
        check(resolved==1&&runner.Summary.score==1000&&runner.Summary.completedHolds==1&&runner.Combo==1,"Hold scores once on completion");
        runner.UpdateHold(4,.5f,true);check(resolved==1,"Repeated tail calls do nothing");
        runner=Runner();runner.TryHit(2.5,.5f);check(runner.UpdateHold(3,.5f,false)==HoldOutcome.Broken&&runner.Summary.brokenHolds==1&&runner.Summary.score==0,"Early release gives one Miss");
        foreach(double error in new[]{.1,.18})
        {
            runner=Runner();runner.TryHit(2.5+error,.5f);runner.UpdateHold(3.5,.5f,true);
            check(runner.Summary.score==(error==.1?700:400),"Hold retains Great/Good head timing");
        }
        runner=Runner();runner.TryHit(2.5,.5f);runner.UpdateHold(2.6,1,true);
        check(runner.UpdateHold(2.75,1,true)==HoldOutcome.Broken,"Sustained height mismatch fails");
        runner=Runner();runner.TryHit(2.5,.5f);runner.UpdateHold(2.6,1,true);runner.UpdateHold(2.65,.5f,true);
        check(runner.UpdateHold(3.5,.5f,true)==HoldOutcome.Completed,"Brief height jitter recovers");
        runner=Runner(.2f);runner.TryHit(2.7,.5f);double adjustedTail=runner.ActiveHold.EndTime;check(runner.UpdateHold(3.6,.5f,true)==HoldOutcome.Holding&&runner.UpdateHold(adjustedTail,.5f,true)==HoldOutcome.Completed,"Judgement offset applies to whole hold");
        runner=Runner();runner.Advance(9,0,false);check(runner.Summary.miss==3&&runner.Summary.brokenHolds==1,"Unhit notes time out exactly once");runner.Advance(10,0,false);check(runner.Summary.miss==3,"Timeout cannot repeat");
        var tail=new GameplayChartRunner(new[]{new ChartNote{beat=2,holdBeats=6}},120,3,.14f,1,0,.12f,new SessionSummary());
        check(Math.Abs(tail.EndTime-4.75)<.001,"End time includes hold tail");
        double now=10;var clock=new GameplaySessionClock(()=>now);clock.Start(10.2);check(Math.Abs(clock.Elapsed+.2)<.001,"Audio-clock lead in");
        now=12;clock.TogglePause(false);double frozen=clock.Elapsed;now=18;check(clock.Elapsed==frozen,"Menu pause freezes chart time");
        clock.TogglePause(false);check(Math.Abs(clock.Elapsed-frozen)<.001&&!clock.IsPaused,"Tap resume preserves chart time");
        now=20;clock.TogglePause(true);frozen=clock.Elapsed;now=22;clock.TogglePause(true);now=28;
        check(clock.WaitingForHoldResume&&clock.Elapsed==frozen,"Hold resume waits with frozen clock");
        clock.TogglePause(true);clock.TogglePause(true);check(clock.Elapsed==frozen,"Reopening pause while waiting does not jump");
        clock.ResumeHold();check(!clock.IsPaused&&clock.Elapsed==frozen,"Reacquiring hold resumes without lost time");now=29;check(Math.Abs(clock.Elapsed-frozen-1)<.001,"Clock advances normally after resume");
        return checks+" chart-runner and session-clock checks passed.";
    }
}
