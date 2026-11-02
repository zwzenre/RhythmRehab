using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class SimplifySelectionHeadings
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(active.isDirty)throw new Exception("Save the current scene first.");
        string original=active.path;var report=new List<string>();
        try
        {
            foreach(string name in new[]{"SongSelection","ModeSelection","ExerciseSelection","DifficultySelection"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var canvas=GameObject.Find("/Canvas").transform;
                var title=canvas.Find("Title").GetComponent<TMP_Text>();var subtitle=canvas.Find("Subtitle").gameObject;
                Undo.RecordObject(title,"Simplify selection heading");Undo.RecordObject(title.rectTransform,"Enlarge selection heading");Undo.RecordObject(subtitle,"Remove repeated top instruction");
                subtitle.SetActive(false);title.fontSize=80;title.enableAutoSizing=false;
                title.rectTransform.anchoredPosition=new Vector2(0,420);title.rectTransform.sizeDelta=new Vector2(1240,130);
                title.ForceMeshUpdate(true);
                if(title.isTextOverflowing)throw new Exception(name+" heading would overflow.");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                report.Add(name+": top subtitle hidden, title enlarged from 58 to 80; footer and song details retained.");
            }
        }
        finally{EditorSceneManager.OpenScene(original);}
        return string.Join("\n",report);
    }
}
