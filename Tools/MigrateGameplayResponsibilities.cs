using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class MigrateGameplayResponsibilities
{
    static int Copy<T>(PrototypeGameplay source, params string[] fields) where T:MonoBehaviour
    {
        var target=source.GetComponent<T>();if(!target)target=source.gameObject.AddComponent<T>();
        var from=new SerializedObject(source);var to=new SerializedObject(target);
        foreach(string name in fields){var value=from.FindProperty(name);if(value==null||to.FindProperty(name)==null)throw new Exception("Missing migration property: "+name);to.CopyFromSerializedProperty(value);}
        to.ApplyModifiedPropertiesWithoutUndo();return fields.Length;
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Save current scene first.");
        if(scene.name!="Gameplay")scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
        var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();
        int fields=Copy<GameplayPlayfield>(game,"playfield","playerMarker","noteViews","noteImages","holdViews","judgementX","spawnX","laneSpacing");
        fields+=Copy<GameplayHud>(game,"scoreLabel","accuracyLabel","comboLabel","feedbackLabel","statusLabel","heightLabel","songLabel","modeLabel","progressFill","remainingTimeLabel","performanceIndicators","wellbeingGameplayTip","pausePanel","scorePulse","judgementMotion");
        fields+=Copy<GameplayAudio>(game,"beatSource","hitSource");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Copied and saved "+fields+" existing Inspector fields into Playfield, HUD and Audio components before removing the original fields.";
    }
}
