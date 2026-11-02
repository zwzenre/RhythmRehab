using System;
using System.IO;
using RhythmRehab;
public static class VerifySessionHistory
{
    public static string Main()
    {
        string dir=Path.Combine(UnityEngine.Application.dataPath,"../Temp/HistoryChecks-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);string path=Path.Combine(dir,"history.json");int checks=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);checks++;};
        try
        {
            var store=new SessionHistoryStore(path);string error;
            check(store.Load(out error).Count==0&&error==null,"Empty first run");
            var first=new SessionSummary{songTitle="Test A",modeLabel="THERAPY · LOW",bpm=100,totalNotes=10,perfect=7,great=1,miss=2,score=7700,duration=45,endedUtc=DateTime.UtcNow.AddMinutes(-1).ToString("O"),heightAttempts=8,correctHeightAttempts=7};
            check(store.TrySave(first,out error),"Save completed session");
            check(store.TrySave(first,out error)&&store.Load(out error).Count==1,"Deduplicate session ID");
            first.score=0;check(store.Load(out error)[0].score==7700,"Immutable saved snapshot");
            var second=new SessionSummary{songTitle="Test B",modeLabel="FREE · HARD",totalNotes=32,perfect=1,endedEarly=true,endedUtc=DateTime.UtcNow.ToString("O")};
            check(store.TrySave(second,out error),"Append early-ended session");
            var loaded=new SessionHistoryStore(path).Load(out error);
            check(loaded.Count==2&&loaded[0].sessionId==second.sessionId,"Reload and newest-first sort");
            check(loaded[1].songTitle=="Test A"&&loaded[1].heightAttempts==8&&loaded[0].endedEarly,"Metadata round trip");
            check(Math.Abs(loaded[1].Accuracy-.78f)<.0001,"Accuracy round trip");
            File.WriteAllText(path,"not valid JSON");string original=File.ReadAllText(path);
            check(store.Load(out error).Count==0&&error!=null,"Corruption reported");
            check(!store.TrySave(second,out error)&&File.ReadAllText(path)==original,"Corruption preserved");
            File.WriteAllText(path,"{\"version\":2,\"sessions\":[]}");
            check(!store.TrySave(second,out error),"Unknown version preserved");
            check(!store.TrySave(new SessionSummary(),out error),"Reject incomplete record");
            return checks+" isolated persistence checks passed; no user history touched.";
        }
        finally {File.Delete(path);if(File.Exists(path+".tmp"))File.Delete(path+".tmp");Directory.Delete(dir);}
    }
}
