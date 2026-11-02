using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using RhythmRehab;
public static class VerifyRefactorResults
{
    static SessionHistoryStore original;
    static readonly FieldInfo Store=typeof(SessionHistoryStore).GetField("defaultStore",BindingFlags.Static|BindingFlags.NonPublic);
    static string path,id;
    public static string Main()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();
        if(!game||game.Summary==null||IrisSceneTransition.IsBusy)throw new Exception("Wait for Gameplay.");
        original=(SessionHistoryStore)Store.GetValue(null);
        path=Path.GetFullPath("Temp/refactor-results-"+Guid.NewGuid().ToString("N")+".json");
        Store.SetValue(null,new SessionHistoryStore(path));id=game.Summary.sessionId;
        game.enabled=false;GameplayEditorAccess.Write(game,"finished",false);game.CompleteSession();
        return "Checking isolated session save and Results navigation.";
    }
    public static string Check()
    {
        try
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Results"||IrisSceneTransition.IsBusy)return "WAIT";
            var saved=SessionHistoryStore.Default.Load(out var error);
            if(error!=null||saved.Count!=1||saved[0].sessionId!=id||SessionSummary.Last.sessionId!=id)throw new Exception("Saved result does not match session.");
            if(saved[0].completedHolds!=SessionSummary.Last.completedHolds||saved[0].score!=SessionSummary.Last.score)throw new Exception("Result counters changed during save.");
            return "PASS Results navigation and isolated JSON save, including hold statistics.";
        }
        finally { if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Results"&&!IrisSceneTransition.IsBusy)Cleanup(); }
    }
    public static void Cleanup()
    {
        Store.SetValue(null,original);
        if(path!=null&&File.Exists(path))File.Delete(path);
        if(path!=null&&File.Exists(path+".tmp"))File.Delete(path+".tmp");
    }
}
