using System;
using RhythmRehab;
public static class VerifyRhythmPreferences
{
 public static string Main()
 {
  int checks=0;Action<bool,string> check=(v,label)=>{if(!v)throw new Exception(label);checks++;};
  check(Math.Abs(RhythmPreferences.TimingError(2.12,2,.12f))<.00001,"Positive offset accepts later press");
  check(Math.Abs(RhythmPreferences.TimingError(1.92,2,-.08f))<.00001,"Negative offset accepts earlier press");
  check(Math.Abs(RhythmPreferences.TimingError(2,2,.12f)+.12)<.00001,"Offset sign");
  check(RhythmRules.Judge(RhythmPreferences.TimingError(2.12,2,.12f),0,.14f)==HitJudgement.Perfect,"Offset Perfect");
  check(RhythmRules.Judge(RhythmPreferences.TimingError(2.30,2,.12f),0,.14f)==HitJudgement.Good,"Offset Good");
  check(RhythmRules.Judge(RhythmPreferences.TimingError(2.12,2,.12f),.5f,.14f)==HitJudgement.Miss,"Height still required");
  check(RhythmPreferences.TravelSeconds(3,1)==3,"Default speed unchanged");
  check(RhythmPreferences.TravelSeconds(3,1.5f)==2,"Faster notes shorten travel");
  check(RhythmPreferences.TravelSeconds(3,.5f)==6,"Slower notes lengthen travel");
  check(RhythmPreferences.TravelSeconds(3,0)==6,"Minimum speed clamp");
  check(RhythmPreferences.TravelSeconds(3,5)==1.5f,"Maximum speed clamp");
  return checks+" judgement-offset and speed checks passed.";
 }
}
