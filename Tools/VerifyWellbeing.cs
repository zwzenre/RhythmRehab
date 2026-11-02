using System;
using System.IO;
using UnityEngine;
using RhythmRehab;
public static class VerifyWellbeing
{
 public static string Main(){
  int checks=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);checks++;};
  string dir=Path.Combine(Application.dataPath,"../Temp/WellbeingChecks-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  try{
   SessionWellbeing.Record(7,7,8,8);var low=SessionWellbeing.Snapshot;check(!low.highPain&&!low.highStress&&!low.reducedPerformanceFeedback,"7 does not trigger high messages");
   SessionWellbeing.Record(8,0,8,8);var pain=SessionWellbeing.Snapshot;check(pain.highPain&&!pain.highStress&&pain.reducedPerformanceFeedback,"Pain threshold includes 8");
   SessionWellbeing.Record(0,8,8,8);var stress=SessionWellbeing.Snapshot;check(!stress.highPain&&stress.highStress&&stress.reducedPerformanceFeedback,"Stress threshold includes 8");
   SessionWellbeing.Record(10,10,8,8);var both=SessionWellbeing.Snapshot;check(both.highPain&&both.highStress,"Both conditions retained");
   both.pain=0;check(SessionWellbeing.Snapshot.pain==10,"Assessment snapshots cannot mutate current state");
   check(SessionWellbeing.GameplayMessage(pain).Contains("discomfort")&&SessionWellbeing.GameplayMessage(stress).Contains("comfortably"),"Condition-specific gameplay messages");
   SessionWellbeing.Skip();var skipped=SessionWellbeing.Snapshot;check(skipped.skipped&&skipped.pain==-1&&skipped.stress==-1&&!skipped.reducedPerformanceFeedback,"Skip differs from zero scores");
   SessionSelection.BeginBrowsing();check(SessionWellbeing.Snapshot==null,"New Start clears previous assessment");
   var store=new SessionHistoryStore(Path.Combine(dir,"sessions.json"));var summary=new SessionSummary{wellbeing=pain,endedUtc=DateTime.UtcNow.ToString("O")};
   check(store.TrySave(summary,out var error),"Session stores wellbeing");
   var restored=new SessionHistoryStore(store.FilePath).Load(out error)[0];check(error==null&&restored.wellbeing.pain==8&&restored.wellbeing.stress==0&&restored.wellbeing.highPain&&DateTimeOffset.TryParse(restored.wellbeing.recordedUtc,out _),"Wellbeing values and timestamp reload");
   check(store.TrySave(new SessionSummary{endedUtc=DateTime.UtcNow.ToString("O")},out error)&&store.Load(out error).Count==2,"Sessions without an assessment remain compatible");
   return checks+" wellbeing boundary, snapshot and local session persistence checks passed; user history untouched.";
  }finally{SessionWellbeing.Reset();Directory.Delete(dir,true);}
 }
}
