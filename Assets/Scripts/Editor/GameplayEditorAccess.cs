using System;
using System.Reflection;
using UnityEditor;
using RhythmRehab;

// Compatibility for the project's older editor probes. Never included in player builds.
public static class GameplayEditorAccess
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    public static SerializedObject Owner(PrototypeGameplay game, string property)
    {
        foreach(var target in new UnityEngine.Object[]{game,game.GetComponent<GameplayPlayfield>(),game.GetComponent<GameplayHud>(),game.GetComponent<GameplayAudio>()})
        {
            if(!target)continue;
            var so=new SerializedObject(target);if(so.FindProperty(property)!=null)return so;
        }
        throw new MissingFieldException("Gameplay property not found: "+property);
    }
    public static object Read(object target,string name)
    {
        if(target is PrototypeGameplay game && game.Notes!=null)
        {
            switch(name)
            {
                case "chart": return game.Notes.Chart;
                case "resolved": return Field(game.Notes,"resolved").GetValue(game.Notes);
                case "summary": return game.Summary;
                case "activeHold": return game.ActiveHoldIndex;
                case "holdState": return game.Notes.ActiveHold;
                case "judgementOffsetSeconds": return game.Notes.JudgementOffset;
                case "approachSeconds": return game.Notes.ApproachSeconds;
                case "timingScale": return game.Notes.TimingScale;
                case "startDsp": return game.Clock.StartDsp;
                case "pauseDsp": return Field(game.Clock,"pausedDsp").GetValue(game.Clock);
                case "paused": return game.Clock.PauseMenuOpen;
                case "waitingForHoldResume": return game.WaitingForHoldResume;
            }
        }
        return Field(FindOwner(target,name),name).GetValue(FindOwner(target,name));
    }
    public static void Write(object target,string name,object value)
    {
        if(target is PrototypeGameplay game && game.Notes!=null)
        {
            if(name=="startDsp"){Field(game.Clock,"startDsp").SetValue(game.Clock,value);return;}
            if(name=="activeHold"){SetProperty(game.Notes,"ActiveHoldIndex",value);return;}
            if(name=="holdState"){SetProperty(game.Notes,"ActiveHold",value);return;}
            if(name=="paused"){SetProperty(game.Clock,"PauseMenuOpen",value);return;}
            if(name=="waitingForHoldResume"){SetProperty(game.Clock,"WaitingForHoldResume",value);return;}
        }
        var owner=FindOwner(target,name);Field(owner,name).SetValue(owner,value);
    }
    public static object ReadProperty(object target,string name)
    {
        if(target is PrototypeGameplay game&&name=="EndTime")return game.Notes.EndTime;
        return target.GetType().GetProperty(name,Flags).GetValue(target);
    }
    public static object Invoke(object target,string name,object[] args)
    {
        if(target is PrototypeGameplay game)
        {
            if(name=="UpdateMarker")
            {
                game.Playfield.MoveMarker(game.CurrentHeight);
                game.Hud.ShowHeight(game.CurrentHeight,(bool)Read(game,"continuousHeights"),game.Notes.HeightTolerance);return null;
            }
            if(name=="HeightColor")return game.Playfield.HeightColor((float)args[0]);
            if(name=="Resolve")return game.Notes.GetType().GetMethod("Resolve",Flags).Invoke(game.Notes,args);
        }
        return target.GetType().GetMethod(name,Flags).Invoke(target,args);
    }
    static FieldInfo Field(object target,string name)=>target.GetType().GetField(name,Flags)??throw new MissingFieldException(target.GetType().Name,name);
    static void SetProperty(object target,string name,object value)=>target.GetType().GetProperty(name,Flags).SetValue(target,value);
    static object FindOwner(object target,string name)
    {
        if(target.GetType().GetField(name,Flags)!=null)return target;
        if(target is PrototypeGameplay game)
            foreach(var component in new object[]{game.Playfield,game.Hud,game.Audio})if(component!=null&&component.GetType().GetField(name,Flags)!=null)return component;
        throw new MissingFieldException(target.GetType().Name,name);
    }
}
