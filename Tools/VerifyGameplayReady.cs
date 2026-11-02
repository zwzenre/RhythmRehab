using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using RhythmRehab;
public static class VerifyGameplayReady
{
    static string status;static int phase,checks;static float started,observed;static PrototypeGameplay first;
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);checks++;}
    public static string Main()
    {
        if(!EditorApplication.isPlaying||IrisSceneTransition.IsBusy)throw new Exception("Enter settled Play mode first.");
        status="WAIT";phase=checks=0;started=(float)EditorApplication.timeSinceStartup;
        EditorApplication.update+=Tick;IrisSceneTransition.Load("Gameplay");return "Watching READY entry, frozen input/audio/time, automatic start, pause and restart.";
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup-started>35)throw new Exception("READY verification timed out.");
            if(!EditorApplication.isPlaying||IrisSceneTransition.IsBusy)return;
            var g=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(!g||g.Notes==null)return;
            var overlay=(CanvasGroup)GameplayEditorAccess.Read(g.Hud,"readyOverlay");
            if(phase==0)
            {
                if(!g.IsStarting)return;
                first=g;observed=Time.realtimeSinceStartup;
                Check(overlay.gameObject.activeSelf&&overlay.blocksRaycasts&&overlay.alpha==1,"READY is visible and blocks clicks");
                Check(overlay.GetComponent<UnityEngine.UI.Image>().color.a==.8f,"Background is 80 percent black");
                Check(overlay.GetComponentInChildren<TMPro.TMP_Text>().text=="READY","Overlay contains only READY");
                Check(g.Elapsed==0&&g.JudgedNotes==0,"Session time and judgements remain zero");
                Check(!g.TryHit(g.Notes.Chart[0].beat*g.Notes.SecondsPerBeat,g.Notes.Chart[0].TargetHeight),"Space cannot score during READY");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Screenshots/Gameplay-Ready.png"));phase=1;
            }
            else if(phase==1)
            {
                if(Time.realtimeSinceStartup-observed<.35f)return;
                Check(g.IsStarting&&g.Elapsed==0,"READY keeps time frozen");
                Check(!((AudioSource)GameplayEditorAccess.Read(g.Audio,"beatSource")).isPlaying,"Beat audio waits for READY");phase=2;
            }
            else if(phase==2)
            {
                if(g.IsStarting||g.Elapsed<.25f)return;
                Check(!overlay.gameObject.activeSelf&&!overlay.blocksRaycasts,"Overlay clears automatically");
                Check(((AudioSource)GameplayEditorAccess.Read(g.Audio,"beatSource")).isPlaying,"Beat starts after READY");
                Check(!((TMPro.TMP_Text)GameplayEditorAccess.Read(g.Hud,"statusLabel")).text.Contains("READY IN"),"No numeric countdown");
                g.enabled=false;g.TogglePause();Check(g.IsPaused,"Normal pause still works");g.TogglePause();Check(!g.IsPaused,"Normal resume still works");
                Check(g.TryHit(g.Notes.Chart[0].beat*g.Notes.SecondsPerBeat,g.Notes.Chart[0].TargetHeight),"Hits work after READY");
                g.Restart();phase=3;
            }
            else if(phase==3)
            {
                if(g==first||!g.IsStarting)return;
                Check(overlay.gameObject.activeSelf&&g.Elapsed==0,"Restart shows READY again before chart time");phase=4;
            }
            else if(phase==4)
            {
                if(g.IsStarting)return;
                g.enabled=false;GameplayEditorAccess.Write(g,"finished",true);
                status="PASS "+checks+" READY sequence and restart checks. No sessions or settings saved.";EditorApplication.update-=Tick;
            }
        }
        catch(Exception e){status="FAIL "+e.Message;EditorApplication.update-=Tick;}
    }
    public static string Status()=>status;
}
