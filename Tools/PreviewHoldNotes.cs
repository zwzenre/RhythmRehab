using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RhythmRehab;
public static class PreviewHoldNotes
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Field(object o,string name)=>GameplayEditorAccess.Read(o,name);
    public static string Main()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(!game||game.Summary==null)throw new Exception("Wait for Gameplay.");
        game.enabled=false;if(game.IsPaused)game.TogglePause();
        var chart=(ChartNote[])Field(game,"chart");var views=(RectTransform[])Field(game,"noteViews");var holds=(HoldNoteView[])Field(game,"holdViews");
        var resolved=(bool[])Field(game,"resolved");int first=Array.FindIndex(chart,n=>n.IsHold);resolved[first]=false;
        GameplayEditorAccess.Write(game,"finished",false);
        double start=chart[first].beat*60.0/game.Summary.bpm+(float)Field(game,"judgementOffsetSeconds");game.TryHit(start,chart[first].TargetHeight);
        GameplayEditorAccess.Write(game,"finished",true);
        foreach(var view in views)view.gameObject.SetActive(false);
        float x=(float)Field(game,"judgementX"),speed=((float)Field(game,"spawnX")-x)/(float)Field(game,"approachSeconds");
        var colour=(Color)GameplayEditorAccess.Invoke(game,"HeightColor",new object[]{chart[first].TargetHeight});
        views[first].gameObject.SetActive(true);views[first].anchoredPosition=new Vector2(x,Mathf.Lerp(-280,280,chart[first].TargetHeight));
        holds[first].Render(chart[first].holdBeats*60/game.Summary.bpm*speed*.55f,colour,true,.45f);
        int next=Array.FindIndex(chart,first+1,n=>n.IsHold);
        if(next>=0){views[next].gameObject.SetActive(true);views[next].anchoredPosition=new Vector2(400,Mathf.Lerp(-280,280,chart[next].TargetHeight));var c=(Color)GameplayEditorAccess.Invoke(game,"HeightColor",new object[]{chart[next].TargetHeight});holds[next].Render(chart[next].holdBeats*60/game.Summary.bpm*speed,c,false,0);}
        int tap=first+1;views[tap].gameObject.SetActive(true);views[tap].anchoredPosition=new Vector2(50,Mathf.Lerp(-280,280,chart[tap].TargetHeight));holds[tap].Hide();
        GameplayEditorAccess.Write(game,"currentHeight",chart[first].TargetHeight);GameplayEditorAccess.Invoke(game,"UpdateMarker",null);
        var human=UnityEngine.Object.FindAnyObjectByType<HumanLegTracker>();human.enabled=false;human.ApplyHeight(chart[first].TargetHeight);
        ((TMPro.TMP_Text)Field(game,"statusLabel")).text="KEEP SPACE HELD • MAINTAIN HEIGHT • 45%";
        ((NoteHitEffects)Field(game,"hitEffects")).Clear();((JudgementTextFeedback)Field(game,"judgementMotion")).SetIdle("",Color.white);
        Canvas.ForceUpdateCanvases();return "Temporary hold-note visual preview. Scene and session history are not saved.";
    }
}
