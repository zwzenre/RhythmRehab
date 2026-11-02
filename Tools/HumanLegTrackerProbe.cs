using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RhythmRehab;
public static class HumanLegTrackerProbe
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static string status,stage;static float began,since;static int checks;
    static PrototypeGameplay game;static HumanLegTracker tracker;
    static object Field(object o,string n)=>GameplayEditorAccess.Read(o,n);
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);checks++;}
    static void Stage(string name){stage=name;since=Time.realtimeSinceStartup;}
    public static string Main()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play first.");
        game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();
        if(game==null||game.Summary==null)throw new Exception("Wait for Gameplay.");
        game.enabled=false;tracker=UnityEngine.Object.FindAnyObjectByType<HumanLegTracker>();
        checks=0;status="RUNNING";began=Time.realtimeSinceStartup;
        try
        {
            Check(tracker,"Tracker component saved in scene");
            var shin=(Transform)Field(tracker,"lowerLeg");var model=(Transform)Field(tracker,"model");
            var foot=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="B-foot.L");
            var farFoot=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="B-foot.R");
            var thigh=shin.parent;var thighRotation=thigh.rotation;var knee=shin.position;var farPosition=farFoot.position;
            var camera=GameObject.Find("HumanLegTrackerStage/LegTrackerCamera").GetComponent<Camera>();
            var view=GameObject.Find("/Canvas/HumanLegTracker/HumanView").GetComponent<UnityEngine.UI.RawImage>();
            Check(camera.orthographic&&camera.targetTexture==view.texture,"Orthographic camera feeds authored UI view");
            Check(camera.cullingMask==(1<<30)&&(Camera.main.cullingMask&(1<<30))==0,"Tracker is isolated from main camera");
            Check(!view.raycastTarget&&view.uvRect.width<0,"View faces right and does not block input");
            float lastY=float.NegativeInfinity;
            foreach(float height in new[]{0f,.25f,.5f,.75f,1f})
            {
                tracker.ApplyHeight(height);
                Check(foot.position.y>lastY,"Foot rises continuously at height "+height);lastY=foot.position.y;
                var vp=camera.WorldToViewportPoint(foot.position);
                Check(vp.x>.02f&&vp.x<.98f&&vp.y>.02f&&vp.y<.98f,"Foot remains inside view at height "+height);
                Check(Vector3.Distance(shin.position,knee)<.0001f&&Quaternion.Angle(thigh.rotation,thighRotation)<.01f,"Knee pivot and seated thigh stay still");
            }
            Check(Vector3.Distance(farFoot.position,farPosition)<.0001f,"Other leg stays resting");
            tracker.ApplyHeight(0);var original=shin.localRotation;for(int i=0;i<50;i++){tracker.ApplyHeight(1);tracker.ApplyHeight(0);}
            Check(Quaternion.Angle(original,shin.localRotation)<.01f,"Repeated movements do not accumulate rotation");
            var line=GameObject.Find("/Canvas/Playfield/JudgementLine").GetComponent<RectTransform>();
            var panel=GameObject.Find("/Canvas/HumanLegTracker").GetComponent<RectTransform>();
            Check(panel.anchoredPosition.x+panel.rect.width/2<line.anchoredPosition.x-40,"Human view leaves a gap before judgement line");
            Check(((float)Field(game,"spawnX")-line.anchoredPosition.x)/1920>.75f,"Over 75% of reference screen remains for approach");
            var chart=(ChartNote[])Field(game,"chart");Array.Clear((bool[])Field(game,"resolved"),0,chart.Length);
            float offset=(float)Field(game,"judgementOffsetSeconds");
            Check(game.TryHit(chart[0].beat*60.0/game.Summary.bpm+offset,chart[0].TargetHeight),"Hit judgement still works");
            var effects=(NoteHitEffects)Field(game,"hitEffects");var ripples=(NoteHitRipples)Field(effects,"ripples");
            var rings=(UnityEngine.UI.RawImage[])Field(ripples,"rings");
            Check(rings.Any(r=>r.enabled&&Mathf.Abs(r.rectTransform.anchoredPosition.x-line.anchoredPosition.x)<.01f),"Hit ripple follows repositioned judgement line");
            GameplayEditorAccess.Write(game,"currentHeight",.25f);tracker.enabled=true;
            EditorApplication.update+=Tick;Stage("follow");return "Checking knee animation, layout, hits, camera isolation and pause.";
        }
        catch(Exception e){status="FAIL "+e.Message;return status;}
    }
    static void Tick()
    {
        try
        {
            if(Time.realtimeSinceStartup-began>15)throw new Exception("Tracker probe timeout");
            if(Time.realtimeSinceStartup-since<.15f)return;
            if(stage=="follow")
            {
                Check(Mathf.Abs(tracker.DisplayedHeight-.25f)<.001f,"Live LateUpdate follows gameplay height");
                game.TogglePause();GameplayEditorAccess.Write(game,"currentHeight",.75f);Stage("pause");
            }
            else if(stage=="pause")
            {
                Check(Mathf.Abs(tracker.DisplayedHeight-.25f)<.001f,"Pause freezes model pose");game.TogglePause();Stage("resume");
            }
            else
            {
                Check(Mathf.Abs(tracker.DisplayedHeight-.75f)<.001f,"Resume restores height tracking");
                status="PASS "+checks+" human tracker checks. No settings or sessions saved.";EditorApplication.update-=Tick;
            }
        }
        catch(Exception e){status="FAIL "+e.Message;EditorApplication.update-=Tick;}
    }
    public static string Status()=>status;
}
