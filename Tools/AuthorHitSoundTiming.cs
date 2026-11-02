using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class AuthorHitSoundTiming
{
 static void Position(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(current.isDirty)throw new Exception("Save current scene before switching.");
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Settings.unity");
  var canvas=GameObject.Find("/Canvas").transform;var audio=canvas.Find("Audio");
  if(audio.Find("HitSoundTiming"))throw new Exception("Timing control already exists.");
  var controller=UnityEngine.Object.FindAnyObjectByType<PrototypeSettings>();
  Position(audio,190,150,1340,310);Position(audio.Find("Title"),-440,125,400,45);
  Position(audio.Find("BeatLabel"),-460,60,280,45);Position(audio.Find("BeatSlider"),100,60,640,40);Position(audio.Find("BeatValue"),540,60,140,45);
  Position(audio.Find("SfxLabel"),-460,-10,280,45);Position(audio.Find("SfxSlider"),100,-10,640,40);Position(audio.Find("SfxValue"),540,-10,140,45);
  var label=UnityEngine.Object.Instantiate(audio.Find("SfxLabel").gameObject,audio);label.name="HitSoundTimingLabel";Position(label.transform,-460,-80,300,45);label.GetComponent<TMP_Text>().text="Hit sound timing";
  var go=UnityEngine.Object.Instantiate(audio.Find("SfxSlider").gameObject,audio);go.name="HitSoundTiming";Position(go.transform,100,-80,640,40);
  var slider=go.GetComponent<Slider>();slider.onValueChanged=new Slider.SliderEvent();slider.minValue=-250;slider.maxValue=250;slider.wholeNumbers=true;slider.SetValueWithoutNotify(0);UnityEventTools.AddPersistentListener(slider.onValueChanged,controller.SaveHitSoundTiming);
  var value=UnityEngine.Object.Instantiate(audio.Find("SfxValue").gameObject,audio);value.name="HitSoundTimingValue";Position(value.transform,540,-80,140,45);value.GetComponent<TMP_Text>().text="0 ms";
  var hint=UnityEngine.Object.Instantiate(audio.Find("SfxLabel").gameObject,audio);hint.name="HitSoundTimingHint";Position(hint.transform,0,-132,1240,38);var text=hint.GetComponent<TMP_Text>();text.fontSize=21;text.alignment=TextAlignmentOptions.Center;text.text="Earlier (-) skips the clip's start. Later (+) adds delay. Try -100 ms for this clap.";
  var buttonGo=UnityEngine.Object.Instantiate(canvas.Find("HomeButton").gameObject,audio);buttonGo.name="TestHitSound";Position(buttonGo.transform,480,125,240,52);var button=buttonGo.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,controller.TestHitSound);foreach(var t in buttonGo.GetComponentsInChildren<TMP_Text>())t.text="TEST SOUND";
  var sourceGo=new GameObject("HitSoundPreview",typeof(AudioSource));sourceGo.transform.SetParent(controller.transform,false);var source=sourceGo.GetComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Assets/mixkit-short-slap-or-clap-2167.wav");
  var config=new SerializedObject(controller);config.FindProperty("hitSoundTiming").objectReferenceValue=slider;config.FindProperty("hitSoundTimingValue").objectReferenceValue=value.GetComponent<TMP_Text>();config.FindProperty("soundPreview").objectReferenceValue=source;config.ApplyModifiedPropertiesWithoutUndo();
  Position(canvas.Find("SystemTitle"),190,-45,1240,45);foreach(var name in new[]{"RhythmRow","CalibrationRow","ConnectionRow"}){var r=(RectTransform)canvas.Find(name);r.anchoredPosition+=new Vector2(0,-40);}
  var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(source.clip));var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Saved hit sound timing slider, signed milliseconds, Test Sound button and readable audio import.";
 }
}
