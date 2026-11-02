using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class AuthorGentleBackgrounds
{
 static void Motion(GameObject go,GentleBackgroundMotion.MotionType type,Vector2 amplitude,float period,float phase)
 {
  var motion=go.GetComponent<GentleBackgroundMotion>();if(!motion)motion=go.AddComponent<GentleBackgroundMotion>();var so=new SerializedObject(motion);
  so.FindProperty("motionType").enumValueIndex=(int)type;so.FindProperty("amplitude").vector2Value=amplitude;so.FindProperty("period").floatValue=period;so.FindProperty("phase").floatValue=phase;so.FindProperty("textureWindow").floatValue=.97f;so.ApplyModifiedPropertiesWithoutUndo();
 }
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var active=SceneManager.GetActiveScene();if(active.isDirty)throw new Exception("Save current scene first.");string original=active.path;var report=new List<string>();
  foreach(var entry in EditorBuildSettings.scenes){if(!entry.enabled||System.IO.Path.GetFileNameWithoutExtension(entry.path)=="Gameplay")continue;
   var scene=EditorSceneManager.OpenScene(entry.path);int count=0;
   foreach(var root in scene.GetRootGameObjects())foreach(var image in root.GetComponentsInChildren<RawImage>(true)){
    if(image.name=="SpaceBackdrop"){
     Motion(image.gameObject,GentleBackgroundMotion.MotionType.TextureDrift,new Vector2(.008f,.005f),36,0);count++;
    }else if(image.transform.parent&&(image.transform.parent.name=="DecorativeClouds"||image.transform.parent.name=="DecorativeStarlight")){
     bool cloud=image.transform.parent.name=="DecorativeClouds";
     int order=image.transform.GetSiblingIndex();Motion(image.gameObject,GentleBackgroundMotion.MotionType.Float,cloud?new Vector2(16,7):new Vector2(7,4),cloud?27+order*4:24+order*3,order*1.7f+.6f);count++;
    }
   }
   if(count==0)throw new Exception("No saved background layers found in "+scene.name);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(scene.name+": "+count+" gently moving layers");
  }
  if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);return string.Join("\n",report)+"\nGameplay untouched. Background tint, UI positions and character preserved.";
 }
}
