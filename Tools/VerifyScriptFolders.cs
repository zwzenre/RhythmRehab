using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class VerifyScriptFolders
{
    public static string Main()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Use saved Edit mode.");
        var scripts=AssetDatabase.FindAssets("t:MonoScript",new[]{"Assets/Scripts"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        if(scripts.Length!=48)throw new Exception("Expected 48 custom scripts, found "+scripts.Length);
        int scenes=0,components=0,buttons=0;string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        try
        {
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
            {
                var scene=EditorSceneManager.OpenScene(entry.path);scenes++;
                foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script: "+scene.name+"/"+t.name);
                    components+=t.GetComponents<MonoBehaviour>().Length;
                    var button=t.GetComponent<UnityEngine.UI.Button>();if(!button)continue;
                    for(int i=0;i<button.onClick.GetPersistentEventCount();i++)
                    {
                        if(!button.onClick.GetPersistentTarget(i))throw new Exception("Missing button target: "+scene.name+"/"+t.name);
                        buttons++;
                    }
                }
            }
        }
        finally{EditorSceneManager.OpenScene(original);}
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Transitions/SceneTransition.prefab");
        if(!prefab||!prefab.GetComponentInChildren<RhythmRehab.IrisSceneTransition>(true))throw new Exception("Iris prefab script missing.");
        var raw=prefab.GetComponentInChildren<UnityEngine.UI.RawImage>(true);
        if(!raw.material||!raw.material.shader||AssetDatabase.GetAssetPath(raw.material.shader)!="Assets/Shaders/IrisTransition.shader")throw new Exception("Iris material shader reference broken.");
        if(typeof(GameplayEditorAccess).Assembly==typeof(RhythmRehab.PrototypeGameplay).Assembly)throw new Exception("Editor helper included in runtime assembly.");
        var lines=scripts.Select(p=>(p.Contains("/Editor/")?"Assets/Editor/":"Assets/Scripts/Runtime/")+Path.GetFileName(p)+"\t"+p+"\t"+AssetDatabase.AssetPathToGUID(p)).ToList();
        lines.Add("Assets/Scripts/Runtime/IrisTransition.shader\tAssets/Shaders/IrisTransition.shader\t"+AssetDatabase.AssetPathToGUID("Assets/Shaders/IrisTransition.shader"));
        File.WriteAllLines("Temp/ScriptFolderMoves.tsv",lines);
        if(File.Exists("Temp/ScriptFolderMoves.json"))File.Delete("Temp/ScriptFolderMoves.json");
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Scripts");EditorGUIUtility.PingObject(Selection.activeObject);
        return "PASS: 48 scripts in feature folders; "+scenes+" scenes, "+components+" components and "+buttons+" persistent button callbacks checked. No missing scripts. Iris prefab/shader connected; editor helper remains editor-only.";
    }
}
