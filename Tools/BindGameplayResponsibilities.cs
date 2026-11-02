using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class BindGameplayResponsibilities
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="Gameplay"||scene.isDirty)throw new Exception("Open saved Gameplay first.");
        var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();var so=new SerializedObject(game);
        so.FindProperty("playfieldView").objectReferenceValue=game.GetComponent<GameplayPlayfield>();
        so.FindProperty("hud").objectReferenceValue=game.GetComponent<GameplayHud>();
        so.FindProperty("audioService").objectReferenceValue=game.GetComponent<GameplayAudio>();
        so.ApplyModifiedPropertiesWithoutUndo();
        foreach(var component in new MonoBehaviour[]{game.GetComponent<GameplayPlayfield>(),game.GetComponent<GameplayHud>(),game.GetComponent<GameplayAudio>()})
        {
            var config=new SerializedObject(component);var property=config.GetIterator();
            while(property.NextVisible(true))if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue==null)
                throw new Exception(component.GetType().Name+" missing reference: "+property.propertyPath);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Coordinator bound to Playfield, HUD and Audio. All migrated object references verified and saved.";
    }
}
