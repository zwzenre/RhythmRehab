using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class PolishWellbeingAssessment
{
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/WellbeingAssessment.unity");int count=0;
  foreach(var root in scene.GetRootGameObjects())foreach(var button in root.GetComponentsInChildren<Button>(true)){
   if(PrefabUtility.IsPartOfPrefabInstance(button.gameObject))PrefabUtility.UnpackPrefabInstance(button.gameObject,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
   bool purple=button.name=="ContinueButton"||button.name=="AcknowledgeButton";
   button.GetComponent<Image>().color=purple?new Color(.55f,.2f,.95f):new Color(.22f,.5f,.8f);
   EditorUtility.SetDirty(button.GetComponent<Image>());
   foreach(var label in button.GetComponentsInChildren<TMP_Text>(true)){label.fontSize=27;label.enableAutoSizing=false;label.rectTransform.localScale=Vector3.one*.75f;}
   count++;
  }
  UnityEngine.Object.FindAnyObjectByType<RhythmRehab.WellbeingAssessmentController>().Refresh(0);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Matched "+count+" assessment buttons to existing menu colours and text scale; default rating messages visible in the Editor.";
 }
}
