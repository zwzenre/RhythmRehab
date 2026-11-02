using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class PolishHitMarker
{
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.name!="Gameplay")throw new Exception("Open Gameplay first.");
  var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();var so=GameplayEditorAccess.Owner(game,"playerMarker");
  var marker=(RectTransform)so.FindProperty("playerMarker").objectReferenceValue;marker.GetComponent<UnityEngine.UI.Image>().color=Color.white;
  int count=0;
  foreach(var graphic in UnityEngine.Object.FindObjectsByType<CanvasParticleGraphic>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None)){
   var config=new SerializedObject(graphic);config.FindProperty("pixelsPerUnit").floatValue=70;config.ApplyModifiedPropertiesWithoutUndo();count++;
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var preview=EditorSceneManager.OpenScene("Assets/Scenes/RhythmCalibration.unity",OpenSceneMode.Additive);
  foreach(var root in preview.GetRootGameObjects())foreach(var controller in root.GetComponentsInChildren<RhythmCalibrationController>(true)){
   var config=new SerializedObject(controller);var dot=(RectTransform)config.FindProperty("marker").objectReferenceValue;dot.GetComponent<UnityEngine.UI.Image>().color=Color.white;
  }
  EditorSceneManager.MarkSceneDirty(preview);EditorSceneManager.SaveScene(preview);EditorSceneManager.CloseScene(preview,true);
  return "Saved white gameplay and calibration markers; doubled all "+count+" click effect layers from 35 to 70 pixels per unit.";
 }
}
