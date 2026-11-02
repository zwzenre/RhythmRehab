using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using RhythmRehab;
public static class UIScreenAudioProbe
{
 static string[] scenes;static int index,checks;static string status,log;static float started;static bool testing;
 static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
 public static string Main(){
  if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
  scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>System.IO.Path.GetFileNameWithoutExtension(s.path)).ToArray();index=checks=0;status="RUNNING";log="";testing=true;EditorApplication.update+=Tick;Next();return "Checking click and background audio on "+scenes.Length+" scenes.";
 }
 static void Next(){started=Time.realtimeSinceStartup;if(!IrisSceneTransition.Load(scenes[index]))throw new Exception("Navigation blocked");}
 static void Tick(){
  if(!testing)return;
  try{
   if(Time.realtimeSinceStartup-started>15)throw new Exception("Audio timeout in "+scenes[index]);
   if(SceneManager.GetActiveScene().name!=scenes[index]||IrisSceneTransition.IsBusy)return;
   var audio=UnityEngine.Object.FindAnyObjectByType<UIScreenAudio>();Check(audio,"Saved screen audio exists");
   var music=(AudioSource)typeof(UIScreenAudio).GetField("backgroundSource",flags).GetValue(audio);
   var clicks=(AudioSource)typeof(UIScreenAudio).GetField("clickSource",flags).GetValue(audio);
   var tracks=(AudioClip[])typeof(UIScreenAudio).GetField("backgroundTracks",flags).GetValue(audio);
   var settings=LocalSettingsStore.Default.Load(out var error);Check(error==null,"Settings readable");
   Check(clicks&&clicks.clip.name=="mixkit-mouse-click-close-1113"&&!clicks.loop&&!clicks.playOnAwake&&clicks.spatialBlend==0,"Correct UI click source");
   Check(Mathf.Abs(clicks.volume-settings.sfxVolume)<.001f,"Clicks follow saved SFX volume");
   var selectables=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Selectable>(true)).ToArray();
   foreach(var control in selectables){var sound=control.GetComponent<UIClickSound>();Check(sound&&typeof(UIClickSound).GetField("screenAudio",flags).GetValue(sound)==audio,"Control has local saved click reference");}
   var button=selectables.First(s=>s is Button&&s.gameObject.activeInHierarchy&&s.IsInteractable());var listener=button.GetComponent<UIClickSound>();
   int before=audio.ClickCount;listener.OnPointerClick(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});Check(audio.ClickCount==before+1,"Left click plays sound");
   listener.OnPointerClick(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Right});Check(audio.ClickCount==before+1,"Right click ignored");
   listener.OnSubmit(new BaseEventData(EventSystem.current));Check(audio.ClickCount==before+2,"Keyboard submit plays click");
   bool enabled=button.interactable;button.interactable=false;listener.OnPointerClick(new PointerEventData(EventSystem.current));Check(audio.ClickCount==before+2,"Disabled controls stay silent");button.interactable=enabled;
   if(scenes[index]=="Gameplay"){
    Check(!music&&tracks.Length==0&&!audio.CurrentTrack&&!audio.BackgroundPlaying,"Gameplay has no menu music");
    var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();game.enabled=false;
    var hit=(AudioSource)GameplayEditorAccess.Read(game,"hitSource");Check(hit.clip.name=="mixkit-short-slap-or-clap-2167","Note-hit clip unchanged");
   }else{
    Check(music&&tracks.Length==3&&tracks.Contains(audio.CurrentTrack),"Random track chosen from three saved music clips");
    Check(music.loop&&music.spatialBlend==0&&audio.BackgroundPlaying,"Selected background track loops and plays");
    Check(Mathf.Abs(music.volume-settings.beatVolume)<.001f,"Background follows saved music volume");
    if(scenes[index]=="RhythmCalibration"){
     var calibration=UnityEngine.Object.FindAnyObjectByType<RhythmCalibrationController>();calibration.TogglePreview();Check(calibration.IsPlaying&&!audio.BackgroundPlaying,"Practice pauses background music");calibration.StopPreview();Check(audio.BackgroundPlaying,"Stopping practice resumes background music");
    }
    log+=scenes[index]+"="+audio.CurrentTrack.name+"; ";
   }
   audio.ApplyVolumes(.21f,.32f);Check(Mathf.Abs(clicks.volume-.32f)<.001f&&(!music||Mathf.Abs(music.volume-.21f)<.001f),"Volume changes apply live");
   index++;if(index==scenes.Length){status="PASS "+checks+" checks; "+log+"Gameplay excluded; no settings or sessions saved.";End();}else Next();
  }catch(Exception ex){status="FAIL "+ex.Message+"; "+log;End();}
 }
 static void End(){testing=false;EditorApplication.update-=Tick;}
 public static string Status()=>status;
}
