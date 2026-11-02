using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using RhythmRehab;
public static class VerifyHoldInputLoop
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static PrototypeGameplay game;static ChartNote[] chart;static RectTransform[] notes;static UnityEngine.UI.Image body;
    static int index,phase,checks,completed;static double start,end;static float width,began;static Vector2 screen;static string status;
    static InputSettings originalSettings;
    static InputSettings.BackgroundBehavior originalBackground;
    static InputSettings.EditorInputBehaviorInPlayMode originalEditorBehaviour;
    static object Field(string name)=>GameplayEditorAccess.Read(game,name);
    static void Set(string name,object value)=>GameplayEditorAccess.Write(game,name,value);
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
    public static string Main()
    {
        game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(!game||game.Summary==null||IrisSceneTransition.IsBusy)throw new Exception("Wait for Gameplay.");
        game.enabled=false;if(game.IsPaused)game.TogglePause();Set("finished",false);Set("activeHold",-1);Set("holdState",null);Set("waitingForHoldResume",false);
        chart=(ChartNote[])Field("chart");var resolved=(bool[])Field("resolved");for(int i=0;i<resolved.Length;i++)resolved[i]=true;
        index=Array.FindIndex(chart,n=>n.IsHold);resolved[index]=false;
        var field=(RectTransform)Field("playfield");screen=RectTransformUtility.WorldToScreenPoint(null,field.TransformPoint(new Vector3(0,Mathf.Lerp(-280,280,chart[index].TargetHeight),0)));
        start=chart[index].beat*60.0/game.Summary.bpm+(float)Field("judgementOffsetSeconds");end=chart[index].EndBeat*60.0/game.Summary.bpm+(float)Field("judgementOffsetSeconds");
        var views=(HoldNoteView[])Field("holdViews");body=(UnityEngine.UI.Image)typeof(HoldNoteView).GetField("body",Flags).GetValue(views[index]);notes=(RectTransform[])Field("noteViews");
        completed=game.Summary.completedHolds;checks=0;phase=0;status="RUNNING";began=Time.realtimeSinceStartup;
        originalSettings=InputSystem.settings;originalBackground=originalSettings.backgroundBehavior;originalEditorBehaviour=originalSettings.editorInputBehaviorInPlayMode;
        originalSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        originalSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        InputSystem.onAfterUpdate+=After;EditorApplication.update+=Timeout;
        return "Checking Space and mouse input inside dynamic input frames.";
    }
    static void After()
    {
        if(InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        try
        {
            InputState.Change(Mouse.current.position,screen,InputUpdateType.Dynamic);
            InputState.Change(Keyboard.current,phase==1||phase==2?new KeyboardState(Key.Space):new KeyboardState(),InputUpdateType.Dynamic);
            if(phase==0){var unused=Keyboard.current.spaceKey.wasPressedThisFrame;phase++;return;}
            double time=phase==1?start:phase==2?(start+end)*.5:end+.01;
            Set("startDsp",AudioSettings.dspTime-time);GameplayEditorAccess.Invoke(game,"Update",null);
            if(phase==1)
            {
                Check(game.ActiveHoldIndex==index,"Space-down starts hold through gameplay Update; pressed="+Keyboard.current.spaceKey.isPressed+" edge="+Keyboard.current.spaceKey.wasPressedThisFrame+" paused="+game.IsPaused+" time="+game.Elapsed+" expected="+start+" height="+game.CurrentHeight+" miss="+game.Summary.miss);
                Check(Math.Abs(game.CurrentHeight-chart[index].TargetHeight)<.005,"Mouse maps to hold height");width=body.rectTransform.rect.width;
            }
            else if(phase==2)
            {
                Check(game.ActiveHoldIndex==index&&notes[index].gameObject.activeSelf,"Held keyboard state sustains visible note");
                Check(Math.Abs(notes[index].anchoredPosition.x-(float)Field("judgementX"))<.01,"Head pins to judgement line");
                Check(body.rectTransform.rect.width<width,"Gameplay Update shrinks remaining body");
            }
            else
            {
                Check(game.ActiveHoldIndex<0&&!notes[index].gameObject.activeSelf&&game.Summary.completedHolds==completed+1,"Space release at tail completes and hides hold");
                status="PASS "+checks+" dynamic keyboard/mouse Update-path checks. No sessions saved.";End();return;
            }
            phase++;
        }
        catch(Exception e){status="FAIL "+(e.InnerException?.Message??e.Message);End();}
    }
    static void Timeout(){if(Time.realtimeSinceStartup-began>10){status="FAIL no dynamic input frames";End();}}
    static void End()
    {
        InputSystem.onAfterUpdate-=After;EditorApplication.update-=Timeout;
        InputState.Change(Keyboard.current,new KeyboardState());
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());game.enabled=false;Set("finished",true);
        // Do not swap temporary InputSettings objects: the Input System destroys replaced defaults.
        EditorApplication.delayCall+=RestoreSettings;
    }
    static void RestoreSettings(){if(originalSettings){originalSettings.backgroundBehavior=originalBackground;originalSettings.editorInputBehaviorInPlayMode=originalEditorBehaviour;}}
    public static string Status()=>status;
}
