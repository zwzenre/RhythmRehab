using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;
public static class AuthorUIScreenAudio
{
 static AudioClip Clip(string file){var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Assets/"+file);if(!clip)throw new Exception("Missing audio: "+file);return clip;}
 static AudioSource Source(Transform parent,string name,bool loop){var go=new GameObject(name,typeof(AudioSource));go.transform.SetParent(parent,false);var s=go.GetComponent<AudioSource>();s.playOnAwake=false;s.loop=loop;s.spatialBlend=0;return s;}
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var active=SceneManager.GetActiveScene();if(active.isDirty)throw new Exception("Save current scene first.");string original=active.path;
  var click=Clip("mixkit-mouse-click-close-1113.wav");
  var tracks=new[]{Clip("orvien-electronic-corporate-612068.mp3"),Clip("serenomusic-chill-electronic-610173.mp3"),Clip("si-xue-de-xing-edm-09-345572.mp3")};
  var report=new List<string>();
  foreach(var path in EditorBuildSettings.scenes){if(!path.enabled)continue;
   var scene=EditorSceneManager.OpenScene(path.path);var old=GameObject.Find("ScreenAudio");if(old)throw new Exception("Already authored audio in "+scene.name);
   var root=new GameObject("ScreenAudio",typeof(UIScreenAudio));var audio=root.GetComponent<UIScreenAudio>();var config=new SerializedObject(audio);
   var clicks=Source(root.transform,"UIClickAudio",false);clicks.clip=click;config.FindProperty("clickSource").objectReferenceValue=clicks;
   if(scene.name!="Gameplay"){
    config.FindProperty("backgroundSource").objectReferenceValue=Source(root.transform,"BackgroundMusic",true);var list=config.FindProperty("backgroundTracks");list.arraySize=3;for(int i=0;i<3;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=tracks[i];
   }
   config.ApplyModifiedPropertiesWithoutUndo();int count=0;
   foreach(var obj in scene.GetRootGameObjects())foreach(var selectable in obj.GetComponentsInChildren<Selectable>(true)){
    var sound=selectable.GetComponent<UIClickSound>();if(!sound)sound=selectable.gameObject.AddComponent<UIClickSound>();var so=new SerializedObject(sound);so.FindProperty("screenAudio").objectReferenceValue=audio;so.ApplyModifiedPropertiesWithoutUndo();count++;
   }
   var calibration=UnityEngine.Object.FindAnyObjectByType<RhythmCalibrationController>();if(calibration){var so=new SerializedObject(calibration);so.FindProperty("screenAudio").objectReferenceValue=audio;so.ApplyModifiedPropertiesWithoutUndo();}
   if(scene.name=="Settings"){
    GameObject.Find("/Canvas/Audio/BeatLabel").GetComponent<TMP_Text>().text="Music / beat volume";
    GameObject.Find("/Canvas/Audio/SfxLabel").GetComponent<TMP_Text>().text="SFX volume";
   }
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(scene.name+": "+count+" click controls; "+(scene.name=="Gameplay"?"no background music":"3 random music choices"));
  }
  if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
  return string.Join("\n",report);
 }
}
