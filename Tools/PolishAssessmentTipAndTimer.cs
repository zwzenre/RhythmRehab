using System;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class PolishAssessmentTipAndTimer
{
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(active.isDirty)throw new Exception("Save current scene first.");string original=active.path;
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/WellbeingAssessment.unity");UnityEngine.Object.FindAnyObjectByType<WellbeingAssessmentController>().Refresh(0);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");var label=GameObject.Find("/Canvas/RemainingTime").GetComponent<TMP_Text>();label.text="--:--";label.rectTransform.anchoredPosition=new Vector2(-56,116);label.rectTransform.sizeDelta=new Vector2(80,30);
  var track=GameObject.Find("/Canvas/ProgressTrack").GetComponent<RectTransform>();track.anchoredPosition=new Vector2(-48,116);track.sizeDelta=new Vector2(-128,6);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);return "Saved neutral assessment tip and compact time-only countdown; thresholds and reduced-feedback behaviour unchanged.";
 }
}
