using System;
using System.IO;
using UnityEngine;

namespace RhythmRehab
{
    [Serializable]
    public sealed class LocalSettings
    {
        public int version = 1;
        public float musicVolume = .85f;
        public float beatVolume = .25f;
        public float sfxVolume = .7f;
        public float judgementOffsetMs;
        public float noteSpeed = 1;
        public float hitSoundOffsetMs;
        public int hitKeyPrimary = (int)UnityEngine.InputSystem.Key.Space;
        public int hitKeySecondary;
        public string lastLeaderboardName;
    }

    // No cached document: every update merges with the latest file on disk.
    public sealed class LocalSettingsStore
    {
        public static LocalSettingsStore Default { get; } = new LocalSettingsStore(
            Path.Combine(Application.persistentDataPath, "settings.json"));
        public string FilePath { get; }
        private readonly bool migratePlayerPrefs;

        public LocalSettingsStore(string path, bool migratePlayerPrefs = true)
        { FilePath = path; this.migratePlayerPrefs = migratePlayerPrefs; }

        public LocalSettings Load(out string error)
        {
            error = null;
            try
            {
                if (!File.Exists(FilePath))
                {
                    var initial = new LocalSettings();
                    if (migratePlayerPrefs)
                    {
                        initial.musicVolume = PlayerPrefs.GetFloat("RhythmRehab.BeatVolume", .85f);
                        initial.sfxVolume = PlayerPrefs.GetFloat("RhythmRehab.SfxVolume", .7f);
                        initial.judgementOffsetMs = PlayerPrefs.GetFloat(RhythmPreferences.OffsetKey, 0);
                        initial.noteSpeed = PlayerPrefs.GetFloat(RhythmPreferences.SpeedKey, 1);
                    }
                    Clamp(initial);
                    Write(initial, out error);
                    return initial;
                }
                var raw = File.ReadAllText(FilePath);
                var settings = JsonUtility.FromJson<LocalSettings>(raw);
                if (settings == null || settings.version != 1 ||
                    !raw.Contains("\"version\"") || !raw.Contains("\"beatVolume\"") ||
                    !raw.Contains("\"sfxVolume\"") || !raw.Contains("\"judgementOffsetMs\"") ||
                    !raw.Contains("\"noteSpeed\"") || !Finite(settings.beatVolume) ||
                    !Finite(settings.musicVolume) || !Finite(settings.sfxVolume) || !Finite(settings.judgementOffsetMs) || !Finite(settings.noteSpeed) || !Finite(settings.hitSoundOffsetMs))
                    throw new InvalidDataException("Unrecognized settings file. The original file has been preserved.");
                if (!raw.Contains("\"musicVolume\""))
                {
                    // The old beat field controlled songs too. Split it once, with quieter note hits.
                    settings.musicVolume = settings.beatVolume <= 0 ? 0 : Mathf.Max(.85f, settings.beatVolume);
                    settings.beatVolume = .25f;
                    Clamp(settings); Write(settings, out error);
                }
                Clamp(settings);
                return settings;
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { error = ex.Message; return new LocalSettings(); }
        }

        public bool TrySaveVolumes(float beat, float sfx, out string error)
        {
            var settings = Load(out error);
            if (error != null) return false;
            settings.beatVolume = beat; settings.sfxVolume = sfx;
            return Write(settings, out error);
        }
        public bool TrySaveAudio(float music, float beat, float sfx, out string error)
        {
            var settings = Load(out error);
            if (error != null) return false;
            settings.musicVolume = music; settings.beatVolume = beat; settings.sfxVolume = sfx;
            return Write(settings, out error);
        }

        public bool TrySaveRhythm(float offsetMs, float speed, out string error)
        {
            var settings = Load(out error);
            if (error != null) return false;
            settings.judgementOffsetMs = offsetMs; settings.noteSpeed = speed;
            return Write(settings, out error);
        }

        public bool TrySaveHitKeys(int primary, int secondary, out string error)
        {
            if (!HitControls.IsAssignable(primary) || (secondary != 0 &&
                (!HitControls.IsAssignable(secondary) || secondary == primary)))
            { error = "Choose up to two different hit keys. Escape is reserved for pause."; return false; }
            var settings = Load(out error);
            if (error != null) return false;
            settings.hitKeyPrimary = primary; settings.hitKeySecondary = secondary;
            return Write(settings, out error);
        }

        public bool TrySaveLeaderboardName(string name, out string error)
        {
            var settings = Load(out error);
            if (error != null) return false;
            settings.lastLeaderboardName = name;
            return Write(settings, out error);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public bool TrySaveCalibration(float offsetMs, float speed, float soundOffsetMs, out string error)
        {
            var settings = Load(out error);
            if (error != null) return false;
            settings.judgementOffsetMs = offsetMs;
            settings.noteSpeed = speed;
            settings.hitSoundOffsetMs = soundOffsetMs;
            return Write(settings, out error);
        }
        public bool TrySaveHitSoundOffset(float offsetMs, out string error)
        {
            var settings = Load(out error);
            if (error != null) return false;
            settings.hitSoundOffsetMs = offsetMs;
            return Write(settings, out error);
        }
        private static void Clamp(LocalSettings settings)
        {
            settings.musicVolume = Mathf.Clamp01(settings.musicVolume);
            settings.beatVolume = Mathf.Clamp01(settings.beatVolume);
            settings.sfxVolume = Mathf.Clamp01(settings.sfxVolume);
            settings.judgementOffsetMs = Mathf.Clamp(settings.judgementOffsetMs, -250, 250);
            settings.noteSpeed = Mathf.Clamp(settings.noteSpeed, .5f, 2);
            settings.hitSoundOffsetMs = Mathf.Clamp(settings.hitSoundOffsetMs, -250, 250);
            if (!HitControls.IsAssignable(settings.hitKeyPrimary)) settings.hitKeyPrimary = (int)UnityEngine.InputSystem.Key.Space;
            if (!HitControls.IsAssignable(settings.hitKeySecondary) || settings.hitKeySecondary == settings.hitKeyPrimary) settings.hitKeySecondary = 0;
        }

        private bool Write(LocalSettings settings, out string error)
        {
            error = null;
            if (!Finite(settings.musicVolume) || !Finite(settings.beatVolume) || !Finite(settings.sfxVolume) ||
                !Finite(settings.judgementOffsetMs) || !Finite(settings.noteSpeed) || !Finite(settings.hitSoundOffsetMs))
            { error = "Settings must contain finite numbers."; return false; }
            Clamp(settings);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath)));
                var temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(settings, true));
                if (File.Exists(FilePath)) File.Replace(temp, FilePath, null);
                else File.Move(temp, FilePath);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { error = ex.Message; return false; }
        }
    }
}
