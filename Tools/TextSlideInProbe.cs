using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using RhythmRehab;
public static class TextSlideInProbe
{
 static string[] scenes;
 static int index,checks;static string status,log;static float began,oldScale;
 static TMP_Text title;static TextSlideIn motion;static Vector2 anchored;static Vector3 baseVertex;
 static bool sawIntermediate,sawIrisWait,haveVertex,haveAnchor;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 public static string Main()=>Begin(new[]{"MainMenu","SongSelection","DifficultySelection","ModeSelection","Results","PerformanceHistory"});
 public static string Settings()=>Begin(new[]{"Settings","RhythmCalibration","SensorCalibration","SensorConnection"});
 static string Begin(string[] pages){
  if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
  scenes=pages;index=checks=0;log="";status="RUNNING";oldScale=Time.timeScale;Time.timeScale=0;
  EditorApplication.update+=Tick;Next();return "Checking "+scenes.Length+" scenes at timeScale=0.";
 }
 static void Next(){
  title=null;motion=null;sawIntermediate=sawIrisWait=haveVertex=haveAnchor=false;began=Time.realtimeSinceStartup;
  if(!IrisSceneTransition.Load(scenes[index]))throw new Exception("Navigation blocked");
 }
 static Vector3 FirstVertex(){
  title.havePropertiesChanged=true;title.ForceMeshUpdate();
  for(int i=0;i<title.textInfo.characterCount;i++){var ch=title.textInfo.characterInfo[i];if(ch.isVisible)return title.textInfo.meshInfo[ch.materialReferenceIndex].vertices[ch.vertexIndex];}
  throw new Exception("Title has no visible characters");
 }
 static byte FirstAlpha(){
  title.havePropertiesChanged=true;title.ForceMeshUpdate();
  for(int i=0;i<title.textInfo.characterCount;i++){var ch=title.textInfo.characterInfo[i];if(ch.isVisible)return title.textInfo.meshInfo[ch.materialReferenceIndex].colors32[ch.vertexIndex].a;}
  throw new Exception("Title has no visible characters");
 }
 static void Tick(){
  try{
   if(Time.realtimeSinceStartup-began>15)throw new Exception("Animation timeout in "+scenes[index]);
   var scene=SceneManager.GetActiveScene();if(scene.name!=scenes[index])return;
   if(!title){
    var texts=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true)).Where(t=>t.GetComponentInParent<Canvas>()).ToArray();
    Check(texts.Length>0&&texts.All(t=>t.GetComponent<TextSlideIn>()),scene.name+" all text has saved animation");
    title=texts.First(t=>t.name==(scene.name=="MainMenu"?"GameTitle":"Title"));motion=title.GetComponent<TextSlideIn>();
   }
   if(IrisSceneTransition.IsBusy){Check(motion.CurrentOffset==new Vector2(-80,0),"Text waits for iris");Check(motion.CurrentOpacity==0&&FirstAlpha()==0,"Text begins completely invisible");sawIrisWait=true;return;}
   if(!haveAnchor){anchored=title.rectTransform.anchoredPosition;haveAnchor=true;}
   Check(title.rectTransform.anchoredPosition==anchored,"Title layout position unchanged");
   if(motion.CurrentOffset.x<-.01f&&motion.CurrentOffset.x>-79.9f){sawIntermediate=true;if(!haveVertex){baseVertex=FirstVertex()-(Vector3)motion.CurrentOffset;haveVertex=true;}}
   if(motion.CurrentOpacity>.1f&&motion.CurrentOpacity<.9f)Check(FirstAlpha()>0&&FirstAlpha()<255,"Rendered text fades smoothly while sliding");
   if(motion.IsAnimating)return;
   Check(sawIrisWait,"Iris wait observed");Check(sawIntermediate&&haveVertex,"Smooth intermediate motion observed");
   Check(motion.CurrentOffset==Vector2.zero,"Text reaches final position");Check(Vector3.Distance(FirstVertex(),baseVertex)<.05f,"Rendered mesh reaches original position without drift");
   Check(motion.CurrentOpacity==1&&FirstAlpha()==255,"Text finishes fully visible");
   var activeMotions=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TextSlideIn>()).Where(t=>t.isActiveAndEnabled).ToArray();
   if(activeMotions.Any(t=>t.IsAnimating))return;
   log+=scene.name+" PASS ("+activeMotions.Length+" active texts); ";index++;
   if(index==scenes.Length){status="PASS "+checks+" frame/scene checks; "+log+"unscaled time verified.";End();}else Next();
  }catch(Exception ex){status="FAIL "+ex.Message+"; "+log;End();}
 }
 static void End(){EditorApplication.update-=Tick;Time.timeScale=oldScale;}
 public static string Status()=>status;
}
