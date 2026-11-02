using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;
public static class AuthorUIInteractionFeedback
{
 static void Bind(MonoBehaviour target,string field,UnityEngine.Object value){var so=target is PrototypeGameplay g?GameplayEditorAccess.Owner(g,field):new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static JudgementTextFeedback Judgement(MonoBehaviour controller,string labelField)
 {
  var config=controller is PrototypeGameplay g?GameplayEditorAccess.Owner(g,labelField):new SerializedObject(controller);var label=(TMP_Text)config.FindProperty(labelField).objectReferenceValue;
  var motion=label.GetComponent<JudgementTextFeedback>();if(!motion)motion=label.gameObject.AddComponent<JudgementTextFeedback>();
  var source=(NoteHitEffects)(controller is PrototypeGameplay gp?GameplayEditorAccess.Owner(gp,"hitEffects"):config).FindProperty("hitEffects").objectReferenceValue;
  if(!label.transform.Find("JudgementSparkles")){
   var effects=UnityEngine.Object.Instantiate(source.gameObject,label.transform,false);effects.name="JudgementSparkles";var r=effects.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=Vector2.zero;r.localScale=Vector3.one;
   foreach(var graphic in effects.GetComponentsInChildren<CanvasParticleGraphic>(true)){var so=new SerializedObject(graphic);so.FindProperty("pixelsPerUnit").floatValue=24;so.ApplyModifiedPropertiesWithoutUndo();}
   Bind(motion,"textEffects",effects.GetComponent<NoteHitEffects>());
  }
  Bind(controller,"judgementMotion",motion);return motion;
 }
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var active=SceneManager.GetActiveScene();if(active.isDirty)throw new Exception("Save current scene first.");string original=active.path;var report=new List<string>();
  foreach(var entry in EditorBuildSettings.scenes){if(!entry.enabled)continue;var scene=EditorSceneManager.OpenScene(entry.path);int count=0;
   Canvas.ForceUpdateCanvases();
   foreach(var root in scene.GetRootGameObjects())foreach(var button in root.GetComponentsInChildren<Button>(true)){
    var hover=button.GetComponent<UIHoverFeedback>();if(!hover)hover=button.gameObject.AddComponent<UIHoverFeedback>();var config=new SerializedObject(hover);var rect=button.GetComponent<RectTransform>();bool card=rect.rect.width>380&&rect.rect.height>150;
    config.FindProperty("hoverScale").floatValue=card?1.025f:1.06f;config.FindProperty("duration").floatValue=.15f;config.FindProperty("colourStrength").floatValue=card?.14f:.2f;
    Color tint=Color.white;if(card){var title=button.transform.Find("Title");if(title&&title.GetComponent<TMP_Text>())tint=title.GetComponent<TMP_Text>().color;}
    config.FindProperty("highlightColor").colorValue=tint;config.ApplyModifiedPropertiesWithoutUndo();count++;
   }
   if(scene.name=="Gameplay"){
    var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();var config=GameplayEditorAccess.Owner(game,"scoreLabel");var score=(TMP_Text)config.FindProperty("scoreLabel").objectReferenceValue;
    var pulse=score.GetComponent<ScorePulse>();if(!pulse)pulse=score.gameObject.AddComponent<ScorePulse>();Bind(game,"scorePulse",pulse);Judgement(game,"feedbackLabel");
   }
   if(scene.name=="RhythmCalibration")Judgement(UnityEngine.Object.FindAnyObjectByType<RhythmCalibrationController>(),"feedback");
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(scene.name+": "+count+" hover targets");
  }
  if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);return string.Join("\n",report)+"\nScore pulse and coloured slide-up/fade judgement effects saved in Gameplay and calibration.";
 }
}
