using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;
public static class MoveHitSoundToCalibration
{
 static void Pos(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
 static Transform Move(Transform t,Scene scene,Transform parent){t.SetParent(null);SceneManager.MoveGameObjectToScene(t.gameObject,scene);t.SetParent(parent,false);return t;}
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var settings=EditorSceneManager.OpenScene("Assets/Scenes/Settings.unity");
  var c=GameObject.Find("/Canvas").transform;var audio=c.Find("Audio");var controller=UnityEngine.Object.FindAnyObjectByType<PrototypeSettings>();
  var slider=audio.Find("HitSoundTiming");if(!slider)throw new Exception("Source timing controls missing.");
  var label=audio.Find("HitSoundTimingLabel");var value=audio.Find("HitSoundTimingValue");var hint=audio.Find("HitSoundTimingHint");var button=audio.Find("TestHitSound");var source=controller.transform.Find("HitSoundPreview");
  var rhythm=EditorSceneManager.OpenScene("Assets/Scenes/RhythmCalibration.unity",OpenSceneMode.Additive);
  Transform canvas=null;RhythmCalibrationController calibration=null;
  foreach(var root in rhythm.GetRootGameObjects()){if(root.name=="Canvas")canvas=root.transform;var found=root.GetComponentInChildren<RhythmCalibrationController>();if(found)calibration=found;}
  var controls=canvas.Find("TimingControls");
  foreach(var t in new[]{slider,label,value,hint,button})Move(t,rhythm,controls);
  Move(source,rhythm,calibration.transform);
  Pos(controls,0,-325,1760,240);
  Pos(controls.Find("OffsetLabel"),-640,78,360,40);Pos(controls.Find("OffsetHint"),-640,42,360,30);Pos(controls.Find("OffsetSlider"),150,75,900,40);Pos(controls.Find("OffsetValue"),735,75,160,50);
  Pos(controls.Find("SpeedLabel"),-640,3,360,40);Pos(controls.Find("SpeedHint"),-640,-33,360,30);Pos(controls.Find("SpeedSlider"),150,0,900,40);Pos(controls.Find("SpeedValue"),735,0,160,50);
  Pos(label,-640,-67,360,40);label.GetComponent<TMP_Text>().text="HIT SOUND TIMING";label.GetComponent<TMP_Text>().fontSize=26;
  Pos(slider,150,-75,900,40);
  // Match the existing calibration sliders, including their authored track widths.
  var previousSlider=slider.GetComponent<Slider>();var newSliderGo=UnityEngine.Object.Instantiate(controls.Find("OffsetSlider").gameObject,controls);newSliderGo.name="HitSoundTiming";
  var soundSlider=newSliderGo.GetComponent<Slider>();soundSlider.onValueChanged=new Slider.SliderEvent();UnityEventTools.AddPersistentListener(soundSlider.onValueChanged,calibration.AdjustHitSound);Pos(newSliderGo.transform,150,-75,900,40);UnityEngine.Object.DestroyImmediate(previousSlider.gameObject);
  Pos(value,735,-75,160,50);value.GetComponent<TMP_Text>().fontSize=30;value.GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
  Pos(hint,150,-110,1060,25);hint.GetComponent<TMP_Text>().fontSize=18;hint.GetComponent<TMP_Text>().text="Earlier (-): skip lead-in · Later (+): add delay · Try -100 ms";
  Pos(button,-640,-105,280,34);var test=button.GetComponent<Button>();test.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(test.onClick,calibration.TestHitSound);foreach(var t in button.GetComponentsInChildren<TMP_Text>())t.fontSize=24;
  foreach(var name in new[]{"PreviewPanel","PracticeInfo"}){var r=(RectTransform)canvas.Find(name);r.anchoredPosition+=new Vector2(0,15);}
  var config=new SerializedObject(calibration);config.FindProperty("hitSoundTiming").objectReferenceValue=soundSlider;config.FindProperty("hitSoundTimingValue").objectReferenceValue=value.GetComponent<TMP_Text>();config.FindProperty("soundPreview").objectReferenceValue=source.GetComponent<AudioSource>();config.ApplyModifiedPropertiesWithoutUndo();
  Pos(audio,190,180,1340,250);Pos(audio.Find("Title"),-440,90,400,45);Pos(audio.Find("BeatLabel"),-460,20,280,45);Pos(audio.Find("BeatSlider"),100,20,640,40);Pos(audio.Find("BeatValue"),540,20,140,45);Pos(audio.Find("SfxLabel"),-460,-65,280,45);Pos(audio.Find("SfxSlider"),100,-65,640,40);Pos(audio.Find("SfxValue"),540,-65,140,45);
  Pos(c.Find("SystemTitle"),190,0,1240,45);foreach(var name in new[]{"RhythmRow","CalibrationRow","ConnectionRow"}){var r=(RectTransform)c.Find(name);r.anchoredPosition+=new Vector2(0,40);}
  EditorSceneManager.MarkSceneDirty(settings);EditorSceneManager.MarkSceneDirty(rhythm);EditorSceneManager.SaveScene(settings);EditorSceneManager.SaveScene(rhythm);SceneManager.SetActiveScene(rhythm);EditorSceneManager.CloseScene(settings,true);
  return "Moved slider, label, hint, Test Sound and audio source to Rhythm Calibration; Settings hub restored. SAVE now commits all three calibration values.";
 }
}
