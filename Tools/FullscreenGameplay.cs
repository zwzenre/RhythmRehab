using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;

public static class FullscreenGameplay
{
 public static string Main()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.name!="Gameplay"||UnityEditor.EditorApplication.isPlaying)throw new Exception("Open Gameplay outside Play mode.");
  var canvas=GameObject.Find("/Canvas").transform;
  var field=(RectTransform)canvas.Find("Playfield");var background=canvas.Find("Background");
  var image=field.GetComponent<Image>();
  Undo.RecordObject(field,"Make gameplay fullscreen");
  Vector2 shift=new Vector2(field.anchoredPosition.x,0);
  // Keep the note origin and marker aligned while expanding their enclosing panel.
  var game=GameObject.Find("/Controllers").GetComponent<PrototypeGameplay>();var so=GameplayEditorAccess.Owner(game,"judgementX");
  so.FindProperty("judgementX").floatValue+=shift.x;
  so.FindProperty("spawnX").floatValue=900;
  so.ApplyModifiedProperties();
  foreach(Transform t in field)
  {
   var rect=(RectTransform)t;Undo.RecordObject(rect,"Keep gameplay elements aligned");
   if(t.name=="Notes")
   {
    rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
   }
   else rect.anchoredPosition+=shift;
  }
  field.anchorMin=Vector2.zero;field.anchorMax=Vector2.one;field.offsetMin=field.offsetMax=Vector2.zero;
  field.SetSiblingIndex(1); // Behind every HUD element and the pause overlay.
  image.raycastTarget=false;
  foreach(var name in new[]{"LowLane","MidLane","HighLane"})
  {
   var lane=(RectTransform)field.Find(name);float y=lane.anchoredPosition.y;
   lane.anchorMin=new Vector2(0,.5f);lane.anchorMax=new Vector2(1,.5f);
   lane.offsetMin=new Vector2(370,y-1);lane.offsetMax=new Vector2(-30,y+1);
  }
  var bgImage=background.GetComponent<Image>();Undo.RecordObject(bgImage,"Use plain gameplay background");
  bgImage.color=image.color;bgImage.raycastTarget=false;
  foreach(Transform child in background){Undo.RecordObject(child.gameObject,"Hide space art in gameplay");child.gameObject.SetActive(false);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  return "Gameplay dark panel now stretches edge to edge; space art disabled in Gameplay; HUD, note references and pause controls retained.";
 }
}
