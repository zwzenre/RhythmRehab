using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RhythmRehab;
public static class VisualLevelsProbe
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static string status,stage;static int level,checks,objectCount;static float began,since,clock;
    static PrototypeGameplay game;static GameplayVisualLevels visuals;static VisualLevelMotion motion;
    static Vector3 position;static Quaternion rotation;
    static FieldInfo[] selectionFields;static object[] selectionValues;
    static object Field(object target,string name)=>GameplayEditorAccess.Read(target,name);
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);checks++;}
    static void Stage(string name){stage=name;since=Time.realtimeSinceStartup;}
    public static string Main()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play first.");
        selectionFields=typeof(SessionSelection).GetFields(BindingFlags.Static|BindingFlags.NonPublic);
        selectionValues=selectionFields.Select(f=>f.GetValue(null)).ToArray();
        checks=0;level=0;status="RUNNING";began=Time.realtimeSinceStartup;
        EditorApplication.update+=Tick;Load();return "Testing automatic selection, distinct motifs, hits, misses, motion and pause across all four themes.";
    }
    static void Load()
    {
        SessionSelection.SelectSong(new SongEntry{id="visual-check",title="Visual check",bpm=120});
        SessionSelection.SelectMode(level==0?SessionMode.Therapy:SessionMode.Free);
        SessionSelection.SelectDifficulty((FreeDifficulty)Math.Max(0,level-1));SessionSelection.SelectHeight(HeightLevel.Low);SessionSelection.Confirm();
        IrisSceneTransition.Load("Gameplay");Stage("load");
    }
    static UnityEngine.UI.Graphic[] AccentGraphics()=>((GameplayVisualLevels.Theme[])Field(visuals,"themes"))[level].hitAccents.Select(a=>(UnityEngine.UI.Graphic)Field(a,"graphic")).ToArray();
    static void Tick()
    {
        try
        {
            if(Time.realtimeSinceStartup-began>50)throw new Exception("Timeout: "+stage);
            if(IrisSceneTransition.IsBusy)return;
            float elapsed=Time.realtimeSinceStartup-since;
            if(stage=="load")
            {
                game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(!game||game.Summary==null)return;
                game.enabled=false;GameplayEditorAccess.Write(game,"finished",false);if(game.IsPaused)game.TogglePause();
                visuals=UnityEngine.Object.FindAnyObjectByType<GameplayVisualLevels>();
                Check(visuals&&visuals.ActiveTheme==level,"Correct theme selected from session choices: "+level);
                var themes=(GameplayVisualLevels.Theme[])Field(visuals,"themes");
                Check(themes.Count(t=>t.root.activeSelf)==1,"Only selected theme is active");
                string motif=new[]{"DistantPlanet","BluePlanet","RingedPlanet","SolarStar"}[level];
                Check(themes[level].root.transform.Find(motif),"Distinct motif present: "+motif);
                Check(visuals.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g=>!g.raycastTarget),"All level decorations ignore input");
                Check(visuals.transform.GetSiblingIndex()==0,"Visual environment stays behind notes and ripples");
                motion=themes[level].root.GetComponentInChildren<VisualLevelMotion>();position=motion.transform.localPosition;rotation=motion.transform.localRotation;
                objectCount=visuals.GetComponentsInChildren<Transform>(true).Length;Stage("motion");
            }
            else if(stage=="motion"&&elapsed>.25f)
            {
                Check(Vector3.Distance(motion.transform.localPosition,position)>.01f||Quaternion.Angle(motion.transform.localRotation,rotation)>.01f,"Theme visibly advances over live frames");
                var chart=(ChartNote[])Field(game,"chart");Array.Clear((bool[])Field(game,"resolved"),0,chart.Length);float offset=(float)Field(game,"judgementOffsetSeconds");
                int before=visuals.SuccessfulReactions;
                Check(game.TryHit(chart[0].beat*60.0/game.Summary.bpm+offset,chart[0].TargetHeight),"Successful note consumed");
                Check(visuals.SuccessfulReactions==before+1,"Successful hit triggers theme reaction");
                Check(AccentGraphics().Count(g=>g.enabled)==(level<=1?1:3),"Correct theme-specific number of accents");
                Check(AccentGraphics().Where(g=>g.enabled).All(g=>g.color.r>.9f&&g.color.g>.7f&&g.color.b<.3f),"Perfect accent stays yellow");
                game.TryHit(chart[1].beat*60.0/game.Summary.bpm+offset,chart[1].TargetHeight>.5f?0:1);
                Check(visuals.SuccessfulReactions==before+1,"Miss does not add a theme reaction");
                visuals.OnSuccessfulHit(Vector2.zero,HitJudgement.Miss,Color.red);
                Check(visuals.SuccessfulReactions==before+1,"Direct Miss is also rejected");
                Check(visuals.GetComponentsInChildren<Transform>(true).Length==objectCount,"Hit effects reuse authored objects");
                Stage("react");
            }
            else if(stage=="react"&&elapsed>.15f)
            {
                Check(visuals.ReactionStrength>0&&visuals.ReactionStrength<1,"Theme reaction fades progressively");
                Check(AccentGraphics().Where(g=>g.enabled).All(g=>g.color.a<.65f),"Hit accents fade rather than covering notes");
                game.TogglePause();clock=visuals.Clock;position=motion.transform.localPosition;rotation=motion.transform.localRotation;Stage("pause");
            }
            else if(stage=="pause"&&elapsed>.2f)
            {
                Check(Mathf.Abs(visuals.Clock-clock)<.0001f,"Pause freezes environment time");
                Check(Vector3.Distance(motion.transform.localPosition,position)<.001f&&Quaternion.Angle(motion.transform.localRotation,rotation)<.01f,"Pause freezes decoration motion");
                game.TogglePause();Stage("fade");
            }
            else if(stage=="fade"&&elapsed>1.3f)
            {
                Check(visuals.Clock>clock&&visuals.ReactionStrength==0,"Motion resumes and reaction settles");
                Check(AccentGraphics().All(g=>!g.enabled),"All hit accents disappear after lifetime");
                if(++level<4)Load();
                else{status="PASS "+checks+" live visual-level checks. No sessions or settings saved.";End();}
            }
        }
        catch(Exception error){status="FAIL "+error.Message+" level="+level+" stage="+stage;End();}
    }
    static void End(){EditorApplication.update-=Tick;for(int i=0;i<selectionFields.Length;i++)selectionFields[i].SetValue(null,selectionValues[i]);}
    public static string Status()=>status;
}
