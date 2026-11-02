using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
public static class OrganizeProjectScripts
{
    [Serializable] public sealed class Move { public string from,to,guid; }
    [Serializable] public sealed class Manifest { public List<Move> assets=new List<Move>(); }
    static readonly Dictionary<string,string[]> Groups=new Dictionary<string,string[]>
    {
        {"Gameplay",new[]{"PrototypeGameplay","GameplayChartRunner","GameplayPlayfield","GameplayHud","GameplaySessionClock","ChartNote","HoldNoteState","HoldNoteView","HumanLegTracker","RhythmRules"}},
        {"Selection",new[]{"SongSelectionController","SelectionPageController","SongCardView","SongCatalog","SessionSelection","ModeGameplayDemo","ModeSelectionPreview"}},
        {"VisualEffects",new[]{"CanvasParticleGraphic","IncomingNoteSparks","NoteHitEffects","NoteHitRipples","GameplayVisualLevels","VisualLevelMotion","VisualLevelPath","VisualLevelHitAccent","GentleBackgroundMotion"}},
        {"UI",new[]{"TextSlideIn","UIHoverFeedback","JudgementTextFeedback","ScorePulse","ResultsPresenter","PerformanceHistoryPresenter"}},
        {"Audio",new[]{"GameplayAudio","UIScreenAudio","UIClickSound","HitSoundTiming"}},
        {"Settings",new[]{"PrototypeSettings","RhythmCalibrationController","RhythmPreferences","SensorFrameworkPresenter"}},
        {"Data",new[]{"SessionHistoryStore","LocalSettingsStore","SessionSummary"}},
        {"Wellbeing",new[]{"WellbeingAssessmentController","SessionWellbeing"}},
        {"Navigation",new[]{"IrisSceneTransition","SceneNavigation"}}
    };
    static void Folder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=path.Substring(0,path.LastIndexOf('/'));Folder(parent);
        AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Use saved Edit mode first.");
        var manifest=new Manifest();
        foreach(var group in Groups)foreach(string name in group.Value)
            manifest.assets.Add(new Move{from="Assets/Scripts/Runtime/"+name+".cs",to="Assets/Scripts/"+group.Key+"/"+name+".cs"});
        manifest.assets.Add(new Move{from="Assets/Editor/GameplayEditorAccess.cs",to="Assets/Scripts/Editor/GameplayEditorAccess.cs"});
        manifest.assets.Add(new Move{from="Assets/Scripts/Runtime/IrisTransition.shader",to="Assets/Shaders/IrisTransition.shader"});
        foreach(var move in manifest.assets)
        {
            move.guid=AssetDatabase.AssetPathToGUID(move.from);
            if(string.IsNullOrEmpty(move.guid)||File.Exists(move.to))throw new Exception("Unexpected source/destination: "+move.from+" -> "+move.to);
        }
        if(Directory.GetFiles("Assets/Scripts/Runtime","*.cs").Length!=47)throw new Exception("New scripts found: review folder plan first.");
        Directory.CreateDirectory("Temp");File.WriteAllLines("Temp/ScriptFolderMoves.tsv",manifest.assets.Select(m=>m.from+"\t"+m.to+"\t"+m.guid));
        foreach(var move in manifest.assets)Folder(move.to.Substring(0,move.to.LastIndexOf('/')));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var completed=new List<Move>();
        AssetDatabase.DisallowAutoRefresh();AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var move in manifest.assets)
            {
                string error=AssetDatabase.MoveAsset(move.from,move.to);
                if(!string.IsNullOrEmpty(error))throw new Exception(error);
                completed.Add(move);
            }
        }
        catch
        {
            for(int i=completed.Count-1;i>=0;i--)AssetDatabase.MoveAsset(completed[i].to,completed[i].from);
            throw;
        }
        finally { AssetDatabase.StopAssetEditing();AssetDatabase.AllowAutoRefresh(); }
        foreach(var move in manifest.assets)if(AssetDatabase.AssetPathToGUID(move.to)!=move.guid)throw new Exception("GUID changed: "+move.to);
        foreach(string folder in new[]{"Assets/Scripts/Runtime","Assets/Editor"})
            if(Directory.Exists(folder)&&!Directory.EnumerateFileSystemEntries(folder).Any())AssetDatabase.DeleteAsset(folder);
        AssetDatabase.SaveAssets();
        return "Moved 47 runtime scripts into 9 feature folders, 1 editor helper into Scripts/Editor, and the iris shader into Assets/Shaders. All 49 asset GUIDs preserved.";
    }
}
