using System;
using System.IO;
using UnityEngine;
using RhythmRehab;
public static class VerifyHitSoundTiming
{
 public static string Main(){
  int checks=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);checks++;};
  var original=AudioClip.Create("Timing fixture",1000,2,1000,false);var samples=new float[2000];samples[200]=.6f;samples[201]=.3f;original.SetData(samples,0);
  var early=HitSoundTiming.Adjust(original,-100);var late=HitSoundTiming.Adjust(original,50);
  var data=new float[early.samples*early.channels];early.GetData(data,0);
  check(early.samples==900&&data[0]==.6f&&data[1]==.3f,"Earlier skips 100 ms with stereo preserved");
  data=new float[late.samples*late.channels];late.GetData(data,0);
  check(late.samples==1050&&data[300]==.6f&&data[301]==.3f&&data[0]==0,"Later adds 50 ms silence");
  check(HitSoundTiming.Adjust(original,0)==original,"Zero preserves original clip");
  UnityEngine.Object.DestroyImmediate(early);UnityEngine.Object.DestroyImmediate(late);UnityEngine.Object.DestroyImmediate(original);
  string dir=Path.Combine(Application.dataPath,"../Temp/SoundTiming-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  try{
   string path=Path.Combine(dir,"settings.json");var store=new LocalSettingsStore(path,false);var settings=store.Load(out var error);
   bool saved=store.TrySaveHitSoundOffset(-104,out error);check(saved,"Save negative sound timing: "+error);
   store.TrySaveVolumes(.2f,.3f,out error);store.TrySaveRhythm(55,1.2f,out error);
   settings=new LocalSettingsStore(path,false).Load(out error);check(settings.hitSoundOffsetMs==-104&&settings.judgementOffsetMs==55&&settings.sfxVolume==.3f,"Reload and independent settings preserved");
   store.TrySaveHitSoundOffset(1000,out error);check(store.Load(out error).hitSoundOffsetMs==250,"Timing range clamped");
   File.WriteAllText(path,"{\"version\":1,\"beatVolume\":0.4,\"sfxVolume\":0.5,\"judgementOffsetMs\":20,\"noteSpeed\":1.5}");
   settings=store.Load(out error);check(error==null&&settings.hitSoundOffsetMs==0&&settings.judgementOffsetMs==20,"Existing settings load with default sound timing");
  }finally{Directory.Delete(dir,true);}
  return checks+" audio-sample and settings checks passed.";
 }
}
