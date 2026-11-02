using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;

public static class CompactGameplayLayout
{
 static RectTransform R(string path)=>GameObject.Find(path).GetComponent<RectTransform>();
 static void Rect(RectTransform r,Vector2 min,Vector2 max,Vector2 pos,Vector2 size)
 {Undo.RecordObject(r,"Compact gameplay layout");r.anchorMin=min;r.anchorMax=max;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;}
 static void Text(Transform t,float size,string value=null)
 {var text=t.GetComponent<TMP_Text>();Undo.RecordObject(text,"Format gameplay label");text.fontSize=size;text.enableAutoSizing=false;if(value!=null)text.text=value;EditorUtility.SetDirty(text);}
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before authoring.");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.name!="Gameplay")throw new Exception("Open Gameplay first.");
  var canvas=GameObject.Find("/Canvas").transform;
  var footer=canvas.Find("Sidebar")??canvas.Find("BottomBar");if(!footer)throw new Exception("Instructions panel missing.");
  Undo.RecordObject(footer.gameObject,"Move instructions to bottom bar");footer.name="BottomBar";
  Rect((RectTransform)footer,new Vector2(0,0),new Vector2(1,0),new Vector2(0,52),new Vector2(0,104));
  Rect((RectTransform)footer.Find("Mode"),new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(210,0),new Vector2(380,78));Text(footer.Find("Mode"),24);
  var oldInstructions=footer.Find("Instructions");Undo.RecordObject(oldInstructions.gameObject,"Merge instructions with controls");oldInstructions.gameObject.SetActive(false);
  var tip=(RectTransform)footer.Find("HoldingTip");Rect(tip,Vector2.one*.5f,Vector2.one*.5f,new Vector2(95,-22),new Vector2(1020,34));
  Text(tip,21,"Match the note height at the line. You can hold a height for consecutive notes.");
  var status=canvas.Find("Status")??footer.Find("Status");Undo.SetTransformParent(status,footer,"Merge controls into bottom bar");
  Rect((RectTransform)status,Vector2.one*.5f,Vector2.one*.5f,new Vector2(95,20),new Vector2(1020,38));Text(status,24);
  var legend=(RectTransform)footer.Find("Legend");Rect(legend,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-175,0),new Vector2(320,65));
  Text(legend,23,"<color=#55FF77>LOW</color>   <color=#FFCC33>MID</color>   <color=#FF557F>HIGH</color>");
  var field=canvas.Find("Playfield");float[] heights={-280,0,280};string[] levels={"Low","Mid","High"};
  for(int i=0;i<3;i++)
  {
   var lane=(RectTransform)field.Find(levels[i]+"Lane");Rect(lane,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(0,heights[i]),new Vector2(-32,2));
   var label=(RectTransform)field.Find(levels[i]+"Label");Rect(label,Vector2.one*.5f,Vector2.one*.5f,new Vector2(-870,heights[i]+35),new Vector2(130,40));Text(label,24);
  }
  for(int i=0;i<7;i++)Rect((RectTransform)field.Find("Grid"+i),Vector2.one*.5f,Vector2.one*.5f,new Vector2(Mathf.Lerp(-720,900,i/6f),0),new Vector2(1,700));
  Rect((RectTransform)field.Find("JudgementLine"),Vector2.one*.5f,Vector2.one*.5f,new Vector2(-720,0),new Vector2(4,700));
  Rect((RectTransform)field.Find("LineLabel"),Vector2.one*.5f,Vector2.one*.5f,new Vector2(-720,-375),new Vector2(350,40));Text(field.Find("LineLabel"),23);
  Rect((RectTransform)field.Find("PlayerMarker"),Vector2.one*.5f,Vector2.one*.5f,new Vector2(-720,-280),new Vector2(70,70));
  Rect((RectTransform)field.Find("HeightValue"),Vector2.one*.5f,Vector2.one*.5f,new Vector2(-590,365),new Vector2(220,70));Text(field.Find("HeightValue"),25);
  Rect((RectTransform)field.Find("Feedback"),Vector2.one*.5f,Vector2.one*.5f,new Vector2(100,70),new Vector2(600,90));
  var so=GameplayEditorAccess.Owner(GameObject.Find("/Controllers").GetComponent<PrototypeGameplay>(),"judgementX");
  so.FindProperty("judgementX").floatValue=-720;so.FindProperty("spawnX").floatValue=900;so.FindProperty("laneSpacing").floatValue=280;so.ApplyModifiedProperties();
  var track=R("/Canvas/ProgressTrack");Rect(track,new Vector2(0,0),new Vector2(1,0),new Vector2(0,116),new Vector2(-32,6));
  var fill=(RectTransform)track.Find("Fill");Rect(fill,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
  var top=new Vector2(.5f,1);
  Rect(R("/Canvas/PrototypeBadge"),top,top,new Vector2(-735,-44),new Vector2(390,50));Text(canvas.Find("PrototypeBadge"),24);
  Rect(R("/Canvas/ScoreCard"),top,top,new Vector2(-345,-45),new Vector2(300,70));
  Rect(R("/Canvas/AccuracyCard"),top,top,new Vector2(755,-45),new Vector2(300,70));
  foreach(var card in new[]{"ScoreCard","AccuracyCard"})
  {
   var t=canvas.Find(card);Rect((RectTransform)t.Find("Caption"),Vector2.one*.5f,Vector2.one*.5f,new Vector2(0,20),new Vector2(280,24));Text(t.Find("Caption"),18);
   Rect((RectTransform)t.Find("Value"),Vector2.one*.5f,Vector2.one*.5f,new Vector2(0,-12),new Vector2(280,42));Text(t.Find("Value"),34);
  }
  Rect(R("/Canvas/PauseButton"),top,top,new Vector2(40,-45),new Vector2(160,58));
  Rect(R("/Canvas/Combo"),top,top,new Vector2(410,-45),new Vector2(260,58));Text(canvas.Find("Combo"),27);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  return "Gameplay lanes now span the screen; judgement line at x=240 in 1920 reference resolution; instructions and controls share a 104px bottom bar; top HUD is 80px tall.";
 }
}
