using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;
public static class AddModeContinue
{
 static void Pos(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="ModeSelection")throw new Exception("Open ModeSelection.");
  var c=GameObject.Find("/Canvas").transform;if(c.Find("ContinueButton"))throw new Exception("Continue button already exists.");
  var controller=UnityEngine.Object.FindAnyObjectByType<SelectionPageController>();var so=new SerializedObject(controller);
  var source=EditorSceneManager.OpenScene("Assets/Scenes/DifficultySelection.unity",OpenSceneMode.Additive);Transform sourceCanvas=null;foreach(var root in source.GetRootGameObjects())if(root.name=="Canvas")sourceCanvas=root.transform;
  var go=UnityEngine.Object.Instantiate(sourceCanvas.Find("ContinueButton").gameObject,c,false);go.name="ContinueButton";Pos(go.transform,650,-450,340,82);
  var button=go.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,controller.ContinueMode);foreach(var text in go.GetComponentsInChildren<TMP_Text>())text.text="CONTINUE";
  var outlines=so.FindProperty("choiceOutlines");var badges=so.FindProperty("choiceBadges");outlines.arraySize=badges.arraySize=2;
  string[] names={"TherapyCard","FreeCard"};
  for(int i=0;i<2;i++){
   var card=c.Find(names[i]);var outline=card.GetComponent<Outline>();if(!outline)outline=card.gameObject.AddComponent<Outline>();outline.effectColor=i==0?new Color(.7f,.3f,1):new Color(.2f,.8f,1);outline.effectDistance=new Vector2(3,-3);outline.enabled=i==0;outlines.GetArrayElementAtIndex(i).objectReferenceValue=outline;
   var badge=UnityEngine.Object.Instantiate(sourceCanvas.Find("Choice0/SelectedBadge").gameObject,card,false);badge.name="SelectedBadge";Pos(badge.transform,255,277,190,30);badge.SetActive(i==0);foreach(var t in badge.GetComponentsInChildren<TMP_Text>())t.fontSize=20;badges.GetArrayElementAtIndex(i).objectReferenceValue=badge;
  }
  var selected=c.Find("Info").GetComponent<TMP_Text>();Pos(c.Find("InfoPanel"),-220,-450,1180,82);Pos(selected.transform,-220,-450,1100,75);selected.text="SELECTED: THERAPY MODE\nChoose your target height next.";selected.fontSize=26;so.FindProperty("selectedLabel").objectReferenceValue=selected;so.ApplyModifiedPropertiesWithoutUndo();
  EditorSceneManager.CloseScene(source,true);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=go;
  return "Mode cards now select with outlines and badges; saved Continue button routes to height or difficulty selection.";
 }
}
