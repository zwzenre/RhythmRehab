using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class AuthorCalibrationHitEffects
{
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var current=SceneManager.GetActiveScene();if(current.isDirty)throw new Exception("Save the current scene first.");
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/RhythmCalibration.unity");var controller=UnityEngine.Object.FindAnyObjectByType<RhythmCalibrationController>();
  var config=new SerializedObject(controller);var field=(RectTransform)config.FindProperty("playfield").objectReferenceValue;
  if(field.Find("NoteHitEffects"))throw new Exception("Preview effects already exist.");
  var gameplay=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity",OpenSceneMode.Additive);
  NoteHitEffects source=null;foreach(var root in gameplay.GetRootGameObjects()){var found=root.GetComponentInChildren<NoteHitEffects>(true);if(found)source=found;}
  if(!source)throw new Exception("Gameplay effects missing.");
  var copy=UnityEngine.Object.Instantiate(source.gameObject,field,false);copy.name="NoteHitEffects";
  var rect=copy.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=Vector2.zero;rect.localScale=Vector3.one;rect.SetAsLastSibling();
  config.FindProperty("hitEffects").objectReferenceValue=copy.GetComponent<NoteHitEffects>();config.ApplyModifiedPropertiesWithoutUndo();
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(gameplay,true);SceneManager.SetActiveScene(scene);Selection.activeGameObject=copy;
  return "Saved four click bursts under RhythmCalibration/Canvas/PreviewPanel/Playfield/NoteHitEffects, matching gameplay colours and size.";
 }
}
