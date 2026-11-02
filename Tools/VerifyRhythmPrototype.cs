using System;
using RhythmRehab;

// Run with the live Unity Editor's `script run` command; never included in a player build.
public static class VerifyRhythmPrototype
{
    public static string Main()
    {
        int checks=0;
        Action<bool,string> check=(ok,name)=> { if(!ok)throw new Exception("Failed: "+name);checks++; };
        check(RhythmRules.Judge(0,0,0.14f)==HitJudgement.Perfect,"on-time hit");
        check(RhythmRules.Judge(-0.08,0,0.14f)==HitJudgement.Perfect,"early perfect boundary");
        check(RhythmRules.Judge(0.08,0,0.14f)==HitJudgement.Perfect,"late perfect boundary");
        check(RhythmRules.Judge(0.1,0,0.14f)==HitJudgement.Great,"great timing");
        check(RhythmRules.Judge(-0.14,0,0.14f)==HitJudgement.Great,"great boundary");
        check(RhythmRules.Judge(0.18,0,0.14f)==HitJudgement.Good,"good timing");
        check(RhythmRules.Judge(0.22,0,0.14f)==HitJudgement.Good,"good boundary");
        check(RhythmRules.Judge(0.221,0,0.14f)==HitJudgement.Miss,"outside timing window");
        check(RhythmRules.Judge(0,0.15f,0.14f)==HitJudgement.Miss,"wrong height");
        check(RhythmRules.Judge(0,0.14f,0.14f)==HitJudgement.Perfect,"height boundary");
        var s=new SessionSummary {totalNotes=8};
        s.Record(HitJudgement.Perfect);s.Record(HitJudgement.Great);s.Record(HitJudgement.Good);s.Record(HitJudgement.Miss);
        check(s.Judged==4,"judgement total");
        check(s.score==2100,"score weights");
        check(Math.Abs(s.Accuracy-0.575f)<0.00001,"weighted accuracy");
        check(Math.Abs(s.Completion-0.5f)<0.00001,"partial session completion");
        check(new SessionSummary().Accuracy==0,"empty accuracy");
        check(new SessionSummary().Completion==0,"empty completion");
        return checks+" rhythm rules and session metric checks passed";
    }
}
