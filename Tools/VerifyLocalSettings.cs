using System;
using System.IO;
using RhythmRehab;
using UnityEngine;

public static class VerifyLocalSettings
{
    public static string Main()
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/SettingsChecks-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "settings.json");
        int checks = 0;
        Action<bool,string> check = (ok, label) => { if (!ok) throw new Exception(label); checks++; };
        try
        {
            var store = new LocalSettingsStore(path, false);
            var initial = store.Load(out var error);
            check(error == null && File.Exists(path), "First launch creates settings JSON");
            check(initial.beatVolume == .65f && initial.sfxVolume == .7f && initial.noteSpeed == 1 && initial.judgementOffsetMs == 0, "Defaults");
            check(store.TrySaveVolumes(.23f, .81f, out error), "Save audio");
            check(store.TrySaveRhythm(-95, 1.65f, out error), "Save rhythm");
            var restored = new LocalSettingsStore(path, false).Load(out error);
            check(error == null && restored.beatVolume == .23f && restored.sfxVolume == .81f, "Audio survives fresh store and rhythm save");
            check(restored.judgementOffsetMs == -95 && restored.noteSpeed == 1.65f, "Rhythm survives fresh store");
            store.TrySaveVolumes(.5f, .4f, out error);
            check(store.Load(out error).noteSpeed == 1.65f, "Audio edits preserve rhythm");
            store.TrySaveVolumes(-1, 3, out error); store.TrySaveRhythm(900, 0, out error);
            restored = store.Load(out error);
            check(restored.beatVolume == 0 && restored.sfxVolume == 1 && restored.judgementOffsetMs == 250 && restored.noteSpeed == .5f, "Ranges clamped: " + JsonUtility.ToJson(restored) + " error=" + error);
            check(!store.TrySaveRhythm(float.NaN, 1, out error), "Reject invalid numeric data");
            File.WriteAllText(path, "broken json");
            check(store.Load(out error).noteSpeed == 1 && error != null, "Corruption falls back safely");
            check(!store.TrySaveVolumes(.1f, .2f, out error) && File.ReadAllText(path) == "broken json", "Corruption preserved");
            File.WriteAllText(path, "{\"version\":2,\"beatVolume\":0.2,\"sfxVolume\":0.3,\"judgementOffsetMs\":0,\"noteSpeed\":1}");
            check(!store.TrySaveRhythm(0, 1, out error), "Unknown schema protected");
            File.Delete(path);
            var migrated = new LocalSettingsStore(path).Load(out error);
            check(error == null && migrated.beatVolume == Mathf.Clamp01(PlayerPrefs.GetFloat("RhythmRehab.BeatVolume", .65f)) &&
                migrated.noteSpeed == Mathf.Clamp(PlayerPrefs.GetFloat(RhythmPreferences.SpeedKey, 1), .5f, 2), "Existing PlayerPrefs migrate");
            return checks + " settings persistence checks passed; user files and preferences unchanged.";
        }
        finally { Directory.Delete(dir, true); }
    }
}
