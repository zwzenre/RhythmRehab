using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;
public static class AuthorTextSlideIns
{
 public static string Main()=>Author(new[]{"MainMenu","SongSelection","DifficultySelection","ModeSelection","Results","PerformanceHistory","Settings","RhythmCalibration","SensorCalibration","SensorConnection","WellbeingAssessment"});
 public static string Settings()=>Author(new[]{"Settings","RhythmCalibration","SensorCalibration","SensorConnection"});
 static string Author(string[] scenes){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var active=SceneManager.GetActiveScene();if(active.isDirty)throw new Exception("Save current scene first.");
  string original=active.path;var results=new List<string>();
  foreach(var name in scenes){
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");Canvas.ForceUpdateCanvases();int count=0;
   foreach(var root in scene.GetRootGameObjects())foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)){
    var canvas=text.GetComponentInParent<Canvas>();if(!canvas)continue;
    var motion=text.GetComponent<TextSlideIn>();if(!motion)motion=text.gameObject.AddComponent<TextSlideIn>();
    var config=new SerializedObject(motion);config.FindProperty("fromOffset").vector2Value=new Vector2(-80,0);config.FindProperty("duration").floatValue=.95f;
    float y=canvas.rootCanvas.transform.InverseTransformPoint(text.rectTransform.position).y;
    config.FindProperty("delay").floatValue=Mathf.Clamp01((430-y)/850)*.22f;config.ApplyModifiedPropertiesWithoutUndo();count++;
   }
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);results.Add(name+": "+count+" texts");
  }
  if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
  return string.Join("\n",results)+"\nSaved staggered fade-and-slide entrances; layouts unchanged.";
 }
}
