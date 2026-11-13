using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using RhythmRehab;
public static class VerifySeparateAudioLive
{
    static int phase,checks;static string status,raw;static float began,at,position;static SongSelectionController owner;static SongPreviewAudio preview;static UIScreenAudio menu;static PrototypeGameplay game;static SongEntry loopSong;static float originalLoopLength;
    static object Field(object o,string n)=>GameplayEditorAccess.Read(o,n);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    public static string Main()
    {
        if(!EditorApplication.isPlaying||IrisSceneTransition.IsBusy)throw new Exception("Wait for Settings Play mode.");
        phase=checks=0;status="RUNNING";began=Time.realtimeSinceStartup;raw=File.ReadAllText(LocalSettingsStore.Default.FilePath);EditorApplication.update+=Tick;
        return "Checking independent sliders, click-to-preview, switching/looping/stopping previews, music/beat sources and calibration gains.";
    }
    static void Tick()
    {
        try
        {
            if(Time.realtimeSinceStartup-began>95)throw new Exception("Audio checks timed out.");
            if(IrisSceneTransition.IsBusy)return;
            if(phase==0)
            {
                var c=UnityEngine.Object.FindAnyObjectByType<PrototypeSettings>();var settings=LocalSettingsStore.Default.Load(out _);var music=(UnityEngine.UI.Slider)Field(c,"musicVolume");var beat=(UnityEngine.UI.Slider)Field(c,"beatVolume");var sfx=(UnityEngine.UI.Slider)Field(c,"sfxVolume");
                try
                {
                    Check(music!=beat&&beat!=sfx&&music.value==settings.musicVolume,"Three independent sliders initialized");
                    music.value=.33f;beat.value=.14f;sfx.value=.52f;var saved=LocalSettingsStore.Default.Load(out var error);
                    Check(error==null&&Math.Abs(saved.musicVolume-.33f)<.001&&Math.Abs(saved.beatVolume-.14f)<.001&&Math.Abs(saved.sfxVolume-.52f)<.001,"Slider changes save independently");
                    var a=UnityEngine.Object.FindAnyObjectByType<UIScreenAudio>();Check(Math.Abs(((AudioSource)Field(a,"backgroundSource")).volume-.33f)<.001&&Math.Abs(((AudioSource)Field(a,"clickSource")).volume-.52f)<.001,"Music and UI clicks use correct gains");
                }
                finally{File.WriteAllText(LocalSettingsStore.Default.FilePath,raw);music.SetValueWithoutNotify(settings.musicVolume);beat.SetValueWithoutNotify(settings.beatVolume);sfx.SetValueWithoutNotify(settings.sfxVolume);UnityEngine.Object.FindAnyObjectByType<UIScreenAudio>().ApplyVolumes(settings.musicVolume,settings.sfxVolume);}
                IrisSceneTransition.Load("SongSelection");phase=1;
            }
            else if(phase==1)
            {
                owner=UnityEngine.Object.FindAnyObjectByType<SongSelectionController>();if(!owner)return;preview=owner.GetComponent<SongPreviewAudio>();menu=UnityEngine.Object.FindAnyObjectByType<UIScreenAudio>();
                Check(preview.CurrentSong==null&&!preview.IsPlaying,"No song preview before clicking");
                GameObject.Find("/Canvas/SongScroll/Viewport/Content/Song0").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();at=Time.realtimeSinceStartup;phase=2;
            }
            else if(phase==2)
            {
                if(Time.realtimeSinceStartup-at<.7f)return;
                Check(preview.IsPlaying&&preview.CurrentSong.title=="Für Elise"&&preview.Source.clip==SessionSelection.Song.audio,"Click starts selected recording");
                Check(preview.Source.time>=8&&!menu.BackgroundPlaying,"Preview seeks past silence and pauses menu music");
                Check(Math.Abs(preview.Source.volume-LocalSettingsStore.Default.Load(out _).musicVolume)<.01,"Preview uses music gain");
                owner.SelectSong(1);at=Time.realtimeSinceStartup;phase=3;
            }
            else if(phase==3)
            {
                if(Time.realtimeSinceStartup-at<.7f)return;
                Check(preview.IsPlaying&&preview.CurrentSong.title=="Canon in D"&&preview.Source.time>=32,"Another card switches to its excerpt");
                Check(GameObject.Find("SongPreview").GetComponents<AudioSource>().Length==1&&!menu.BackgroundPlaying,"Single preview source prevents overlapping songs");
                preview.StopPreview();Check(!preview.IsPlaying&&preview.CurrentSong==null&&menu.BackgroundPlaying,"Stopping preview restores menu music");
                owner.SelectSong(3);Check(preview.CurrentSong.title=="Rondo K.545"&&!menu.BackgroundPlaying,"Fourth song starts its own preview");
                at=Time.realtimeSinceStartup;phase=12;
            }
            else if(phase==12)
            {
                if(Time.realtimeSinceStartup-at<.7f)return;
                Check(preview.IsPlaying&&preview.CurrentSong.title=="Rondo K.545"&&preview.Source.time>=8,"Mozart card plays its actual recording");
                loopSong=((SongCatalog)Field(owner,"catalog")).songs[4];originalLoopLength=loopSong.previewDurationSeconds;loopSong.previewDurationSeconds=3;owner.SelectSong(4);at=Time.realtimeSinceStartup;phase=4;
            }
            else if(phase==4)
            {
                if(Time.realtimeSinceStartup-at<3.8f)return;
                Check(preview.IsPlaying&&preview.CurrentSong.title=="Prelude in E minor"&&preview.Source.time<11,"Preview excerpt loops with fade instead of ending");
                loopSong.previewDurationSeconds=originalLoopLength;loopSong=null;owner.SelectSong(4);owner.Continue();
                Check(!preview.IsPlaying&&preview.CurrentSong==null,"Continue stops preview immediately");phase=5;
            }
            else if(phase==5)
            {
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="ModeSelection")return;
                Check(UnityEngine.Object.FindAnyObjectByType<SongPreviewAudio>()==null,"Preview does not persist into other screens");
                SessionSelection.SelectMode(SessionMode.Free);SessionSelection.SelectDifficulty(FreeDifficulty.Easy);SessionSelection.Confirm();IrisSceneTransition.Load("Gameplay");phase=6;
            }
            else if(phase==6)
            {
                game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(!game||game.IsStarting)return;game.enabled=false;if(game.IsPaused)game.TogglePause();
                var s=LocalSettingsStore.Default.Load(out _);Check(game.Audio.MusicSource!=game.Audio.BeatSource&&game.Audio.MusicSource.clip==SessionSelection.Song.audio,"Gameplay music has its own authored source");
                Check(Math.Abs(game.Audio.MusicSource.volume-s.musicVolume)<.001&&Math.Abs(game.Audio.HitSource.volume-s.beatVolume)<.001&&Math.Abs(game.Audio.BeatSource.volume-s.beatVolume)<.001,"Music and hits/metronome use independent gains");
                Check(!game.Audio.BeatSource.isPlaying,"No extra metronome plays over a real song");phase=7;
            }
            else if(phase==7)
            {
                if(game.IsPaused)game.TogglePause();if(game.Elapsed<6.5f)return;
                Check(game.Audio.MusicSource.isPlaying&&Math.Abs(game.Audio.MusicSource.time-(game.Elapsed-4))<.16,"Dedicated music source stays synchronized");
                game.TogglePause();position=game.Audio.MusicSource.time;at=Time.realtimeSinceStartup;phase=8;
            }
            else if(phase==8)
            {
                if(Time.realtimeSinceStartup-at<.25f)return;
                Check(Math.Abs(game.Audio.MusicSource.time-position)<.04,"Dedicated song source pauses");game.TogglePause();Check(!game.IsPaused,"Song playback resumes");
                game.Audio.Stop();var s=LocalSettingsStore.Default.Load(out _);game.Audio.Initialize(s,120,5,AudioSettings.dspTime+.1);at=Time.realtimeSinceStartup;phase=9;
            }
            else if(phase==9)
            {
                if(Time.realtimeSinceStartup-at<.3f)return;
                Check(game.Audio.BeatSource.isPlaying&&!game.Audio.MusicSource.isPlaying,"Tempo placeholders use only the quiet beat source");game.Audio.Stop();GameplayEditorAccess.Write(game,"finished",true);IrisSceneTransition.Load("RhythmCalibration");phase=10;
            }
            else if(phase==10)
            {
                var c=UnityEngine.Object.FindAnyObjectByType<RhythmCalibrationController>();if(!c)return;float beat=LocalSettingsStore.Default.Load(out _).beatVolume;
                Check(Math.Abs(((AudioSource)Field(c,"metronome")).volume-beat)<.001&&Math.Abs(((AudioSource)Field(c,"soundPreview")).volume-beat)<.001,"Calibration beat and hit preview respect beat gain");
                Check(File.ReadAllText(LocalSettingsStore.Default.FilePath)==raw,"User settings fully restored after tests");IrisSceneTransition.Load("SongSelection");phase=11;
            }
            else if(phase==11)
            {
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="SongSelection")return;
                status="PASS "+checks+" audio controls, preview lifecycle, looping, music synchronization and calibration routing checks. User settings restored; no sessions saved.";EditorApplication.update-=Tick;
            }
        }
        catch(Exception e)
        {
            status="FAIL "+e.Message;EditorApplication.update-=Tick;
            File.WriteAllText(LocalSettingsStore.Default.FilePath,raw);if(loopSong!=null)loopSong.previewDurationSeconds=originalLoopLength;
            if(preview)preview.StopPreview();if(game){game.enabled=false;GameplayEditorAccess.Write(game,"finished",true);game.Audio.Stop();}
        }
    }
    public static string Status()=>status;
}
