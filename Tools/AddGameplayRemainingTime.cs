using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;
public static class AddGameplayRemainingTime
{
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(current.isDirty)throw new Exception("Save current scene first.");
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();var config=GameplayEditorAccess.Owner(game,"remainingTimeLabel");
  var canvas=GameObject.Find("/Canvas").transform;var track=(RectTransform)canvas.Find("ProgressTrack");if(canvas.Find("RemainingTime"))throw new Exception("Remaining time already authored.");
  track.anchoredPosition=new Vector2(-48,116);track.sizeDelta=new Vector2(-128,6);
  var source=(TMP_Text)config.FindProperty("scoreLabel").objectReferenceValue;var go=UnityEngine.Object.Instantiate(source.gameObject,canvas,false);go.name="RemainingTime";
  var label=go.GetComponent<TMP_Text>();var r=label.rectTransform;r.anchorMin=r.anchorMax=new Vector2(1,0);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(-56,116);r.sizeDelta=new Vector2(80,30);r.localScale=Vector3.one;
  label.text="--:--";label.fontSize=22;label.enableAutoSizing=false;label.alignment=TextAlignmentOptions.Right;label.margin=Vector4.zero;label.color=new Color(.68f,.73f,.85f);label.raycastTarget=false;
  config.FindProperty("remainingTimeLabel").objectReferenceValue=label;config.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Saved countdown beside progress line, with space reserved on the right; chart clock pauses with gameplay.";
 }
}
