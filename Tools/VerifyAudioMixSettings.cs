using System;
using System.IO;
using UnityEngine;
using RhythmRehab;
public static class VerifyAudioMixSettings
{
    public static string Main()
    {
        int checks=0;Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);checks++;};
        string path=Path.GetFullPath("Temp/audio-mix-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            var store=new LocalSettingsStore(path,false);var s=store.Load(out var error);
            check(error==null&&s.musicVolume==.85f&&s.beatVolume==.25f,"Balanced independent defaults");
            check(store.TrySaveAudio(.91f,.12f,.43f,out error),"Save three gains");
            s=new LocalSettingsStore(path,false).Load(out error);check(s.musicVolume==.91f&&s.beatVolume==.12f&&s.sfxVolume==.43f,"All gains persist independently");
            store.TrySaveCalibration(65,1.25f,-35,out error);s=store.Load(out error);check(s.musicVolume==.91f&&s.beatVolume==.12f&&s.sfxVolume==.43f,"Calibration preserves all gains");
            store.TrySaveVolumes(.2f,.3f,out error);s=store.Load(out error);check(s.musicVolume==.91f,"Legacy audio update preserves music");
            check(!store.TrySaveAudio(float.NaN,.2f,.3f,out error),"Reject invalid music gain");
            store.TrySaveAudio(2,-1,3,out error);s=store.Load(out error);check(s.musicVolume==1&&s.beatVolume==0&&s.sfxVolume==1,"Clamp all gains");
            File.WriteAllText(path,"{\"version\":1,\"beatVolume\":0.65,\"sfxVolume\":0.4,\"judgementOffsetMs\":45,\"noteSpeed\":1.5,\"hitSoundOffsetMs\":-25}");
            s=store.Load(out error);check(error==null&&s.musicVolume==.85f&&s.beatVolume==.25f&&s.sfxVolume==.4f,"Old shared volume migrates to quieter beat mix");
            check(s.judgementOffsetMs==45&&s.noteSpeed==1.5f&&s.hitSoundOffsetMs==-25,"Migration retains calibration");
            check(File.ReadAllText(path).Contains("\"musicVolume\""),"Migration is persisted once");
            store.TrySaveAudio(.6f,.8f,.4f,out error);s=store.Load(out error);check(s.musicVolume==.6f&&s.beatVolume==.8f,"New independent preferences are never migrated again");
            File.WriteAllText(path,"not json");check(!store.TrySaveAudio(.5f,.2f,.3f,out error)&&File.ReadAllText(path)=="not json","Unreadable user file preserved");
            return "PASS "+checks+" separate-volume persistence, migration and calibration checks. User settings unchanged.";
        }
        finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".tmp"))File.Delete(path+".tmp");}
    }
}
