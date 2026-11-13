using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RhythmRehab;
public static class VerifyClassicalLive
{
    static string status,path;static int phase,checks,expectedNotes;static float began,frozen,observed;static PrototypeGameplay game;
    static SessionHistoryStore original;static readonly FieldInfo Store=typeof(SessionHistoryStore).GetField("defaultStore",BindingFlags.Static|BindingFlags.NonPublic);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    static object Field(object target,string name)=>GameplayEditorAccess.Read(target,name);
    public static string Main()
    {
        if(!EditorApplication.isPlaying||IrisSceneTransition.IsBusy)throw new Exception("Wait for SongSelection Play mode.");
        checks=phase=0;expectedNotes=AssetDatabase.LoadAssetAtPath<SongCatalog>("Assets/Data/ClassicalSongs.asset").songs[0].hardChart.notes.Length;status="RUNNING";began=Time.realtimeSinceStartup;
        original=(SessionHistoryStore)Store.GetValue(null);path=Path.GetFullPath("Temp/classical-live-"+Guid.NewGuid().ToString("N")+".json");Store.SetValue(null,new SessionHistoryStore(path));
        EditorApplication.update+=Tick;return "Checking classical cards, real audio sync, lead-in pause, long-chart slot reuse, full-song scoring and isolated Results save.";
    }
    static void Tick()
    {
        try
        {
            if(Time.realtimeSinceStartup-began>100)throw new Exception("Live song checks timed out.");
            if(IrisSceneTransition.IsBusy)return;
            if(phase==0)
            {
                if(Time.realtimeSinceStartup-began<1.2f)return;
                var owner=UnityEngine.Object.FindAnyObjectByType<SongSelectionController>();var cards=(SongCardView[])Field(owner,"cards");
                Check(cards.Length==5&&cards.All(c=>c.Song.audio&&c.Song.cover),"Five real songs and covers bound");
                Check(cards.All(c=>((TMPro.TMP_Text)Field(c,"tempo")).text.Contains("CLASSICAL")),"Classical cards show actual song duration");
                Check(cards.All(c=>((UnityEngine.UI.Image)Field(c,"cover")).sprite==c.Song.cover),"Downloaded cover sprites shown");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Screenshots/Classical-SongSelection.png"));
                SessionSelection.SelectSong(cards[0].Song);SessionSelection.SelectMode(SessionMode.Free);SessionSelection.SelectDifficulty(FreeDifficulty.Hard);SessionSelection.Confirm();IrisSceneTransition.Load("Gameplay");phase=1;
            }
            else if(phase==1)
            {
                game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(!game||game.IsStarting||game.Notes==null)return;
                game.enabled=false;if(game.IsPaused)game.TogglePause();
                Check(game.Notes.Chart.Length==expectedNotes&&game.Playfield.Capacity==32,"Full chart uses 32 saved note slots");
                var source=(AudioSource)Field(game.Audio,"musicSource");Check(source.clip==SessionSelection.Song.audio,"Selected MP3 replaces metronome");
                Check(Math.Abs(game.Notes.EndTime-SessionSelection.Song.audio.length-4)<.02,"Progress and result duration include the full recording");
                game.TogglePause();frozen=game.Elapsed;observed=Time.realtimeSinceStartup;phase=2;
            }
            else if(phase==2)
            {
                if(Time.realtimeSinceStartup-observed<.3f)return;
                Check(game.IsPaused&&Math.Abs(game.Elapsed-frozen)<.005,"Pause freezes the pre-music lead-in");
                game.TogglePause();Check(!game.IsPaused,"Lead-in resumes");phase=3;
            }
            else if(phase==3)
            {
                if(game.IsPaused)game.TogglePause();
                if(game.Elapsed<6.5f)return;
                var source=(AudioSource)Field(game.Audio,"musicSource");
                Check(source.isPlaying&&Math.Abs(source.time-(game.Elapsed-4))<.16,"Actual MP3 playback stays aligned with chart DSP time after pause");
                game.TogglePause();frozen=source.time;observed=Time.realtimeSinceStartup;phase=4;
            }
            else if(phase==4)
            {
                if(Time.realtimeSinceStartup-observed<.3f)return;
                var source=(AudioSource)Field(game.Audio,"musicSource");Check(Math.Abs(source.time-frozen)<.04,"Music playback pauses with the session");game.TogglePause();
                var catalog=AssetDatabase.LoadAssetAtPath<SongCatalog>("Assets/Data/ClassicalSongs.asset");
                var views=(RectTransform[])Field(game.Playfield,"noteViews");var holds=(HoldNoteView[])Field(game.Playfield,"holdViews");
                foreach(var song in catalog.songs)foreach(var difficulty in new[]{FreeDifficulty.Easy,FreeDifficulty.Medium,FreeDifficulty.Hard})
                {
                    var p=SessionSelection.BuildProfile(song,SessionMode.Free,HeightLevel.Low,difficulty);var runner=new GameplayChartRunner(p.chart,song.bpm,p.approachSeconds,p.heightTolerance,p.timingScale,0,.12f,new SessionSummary{bpm=song.bpm},p.durationSeconds);
                    Check(game.Playfield.CanDisplay(p.chart,song.bpm,p.approachSeconds,runner.HitWindow,0),"Authored pool has enough simultaneous slots");
                    game.Playfield.Initialize(p.chart);
                    foreach(int index in new[]{0,Math.Min(40,p.chart.Length-1),p.chart.Length/2,p.chart.Length-1}.Distinct().OrderBy(i=>i))
                    {
                        float time=p.chart[index].beat*runner.SecondsPerBeat;runner.Advance(time-.3,0,false);game.Playfield.Render(runner,time);
                        var view=views[index%views.Length];Check(view.gameObject.activeSelf&&Math.Abs(view.anchoredPosition.x-game.Playfield.JudgementX)<.02&&Math.Abs(view.anchoredPosition.y-game.Playfield.PositionAt(p.chart[index].TargetHeight).y)<.02,"Later notes reuse correct slot and height");
                    }
                    int hi=Array.FindLastIndex(p.chart,n=>n.IsHold);runner=new GameplayChartRunner(p.chart,song.bpm,p.approachSeconds,p.heightTolerance,p.timingScale,0,.12f,new SessionSummary());
                    runner.TryHit(p.chart[hi].beat*runner.SecondsPerBeat,p.chart[hi].TargetHeight);game.Playfield.Render(runner,(p.chart[hi].beat+p.chart[hi].holdBeats*.5f)*runner.SecondsPerBeat);
                    Check(holds[hi%holds.Length].gameObject.activeInHierarchy,"Reused hold graphic works beyond slot 32");
                }
                game.Audio.Stop();game.Playfield.Initialize(game.Notes.Chart);float previewTime=game.Notes.Chart[40].beat*game.Notes.SecondsPerBeat-1;
                GameplayEditorAccess.Write(game,"startDsp",AudioSettings.dspTime-previewTime);game.Playfield.Render(game.Notes,previewTime);game.Playfield.MoveMarker(.25f);game.Hud.UpdateTimeline(game.Notes,previewTime);observed=Time.realtimeSinceStartup;phase=5;
            }
            else if(phase==5)
            {
                if(Time.realtimeSinceStartup-observed<.3f)return;
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Screenshots/Classical-Gameplay.png"));
                observed=Time.realtimeSinceStartup;phase=8;
            }
            else if(phase==8)
            {
                if(Time.realtimeSinceStartup-observed<.3f)return;
                ((AudioSource)Field(game.Audio,"hitSource")).mute=true;
                foreach(var note in game.Notes.Chart){Check(game.TryHit(note.beat*game.Notes.SecondsPerBeat+game.Notes.JudgementOffset,note.TargetHeight),"Live full-song head scores");if(note.IsHold)Check(game.UpdateActiveHold(note.EndBeat*game.Notes.SecondsPerBeat+game.Notes.JudgementOffset,note.TargetHeight,true)==HoldOutcome.Completed,"Live full-song hold completes");}
                Check(game.Summary.perfect==expectedNotes&&game.Summary.score==expectedNotes*1000,"Live chart scores every note exactly once");
                GameplayEditorAccess.Write(game,"startDsp",AudioSettings.dspTime-game.Notes.EndTime);game.CompleteSession();phase=6;
            }
            else if(phase==6)
            {
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Results")return;
                var saved=SessionHistoryStore.Default.Load(out var error);Check(error==null&&saved.Count==1&&saved[0].songTitle=="Für Elise"&&saved[0].score==expectedNotes*1000,"Real song results and full-chart statistics save to isolated JSON");
                Cleanup();IrisSceneTransition.Load("SongSelection");phase=7;
            }
            else if(phase==7)
            {
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="SongSelection")return;
                status="PASS "+checks+" live classical-song, pause/audio, pool-reuse and result-save checks. Real history preserved.";EditorApplication.update-=Tick;
            }
        }
        catch(Exception e){status="FAIL "+e.Message;EditorApplication.update-=Tick;Cleanup();if(game){game.enabled=false;GameplayEditorAccess.Write(game,"finished",true);}}
    }
    static void Cleanup(){Store.SetValue(null,original);if(path!=null&&File.Exists(path))File.Delete(path);if(path!=null&&File.Exists(path+".tmp"))File.Delete(path+".tmp");}
    public static string Status()=>status;
}
