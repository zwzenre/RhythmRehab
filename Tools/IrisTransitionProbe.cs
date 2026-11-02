using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RhythmRehab;

public static class IrisTransitionProbe
{
 static string log="";static string target;static float started;static int checks;static string previous;
 static bool sawClose,sawLoad,sawOpen,waitedForGameplay;
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);checks++;}
 public static string Begin(string scene)
 {
  target=scene;started=Time.realtimeSinceStartup;checks=0;previous="";log="";sawClose=sawLoad=sawOpen=waitedForGameplay=false;
  Time.timeScale=0;
  Check(IrisSceneTransition.Load(scene),"First navigation accepted");
  Check(!IrisSceneTransition.Load("Settings"),"Second navigation blocked");
  EditorApplication.update+=Tick;return "Probe running to "+scene;
 }
 static void Tick()
 {
  try
  {
   var t=(IrisSceneTransition)typeof(IrisSceneTransition).GetField("instance",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
   if(t==null)return;
   if(t.Phase!=previous){log+=t.Phase+" ";previous=t.Phase;}
   var image=t.GetComponentInChildren<RawImage>(true);
   if(t.Phase=="Closing"){sawClose=true;Check(image.raycastTarget,"Closing blocks clicks");}
   if(t.Phase=="Loading"){sawLoad=true;Check(t.Radius<0,"Scene load hidden behind iris");}
   if(t.Phase=="Opening")
   {
    sawOpen=true;Check(image.raycastTarget,"Opening blocks clicks");
    var g=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();
    if(g!=null){Check(g.Summary==null,"Gameplay waits until open");waitedForGameplay=true;}
   }
   if(t.Phase=="Idle")
   {
    Check(SceneManager.GetActiveScene().name==target,"Expected destination scene");
    Check(sawClose&&sawLoad&&sawOpen,"All iris phases observed: "+log);
    Check(!image.enabled&&!image.raycastTarget,"Overlay clears and releases clicks");
    Check(UnityEngine.Object.FindObjectsByType<IrisSceneTransition>().Length==1,"One persistent transition after scene load");
    if(target=="Gameplay")Check(waitedForGameplay,"Gameplay did not start behind iris");
    log="PASS "+checks+" checks; "+log+"; timeScale=0; waitedForGameplay="+waitedForGameplay;
    End();
   }
   else if(Time.realtimeSinceStartup-started>12)throw new Exception("Transition timeout: "+log);
  }
  catch(Exception ex){log="FAIL "+ex.Message;End();}
 }
 static void End(){EditorApplication.update-=Tick;Time.timeScale=1;}
 public static string Main()=>Begin("SongSelection");
 public static string ToGameplay()=>Begin("Gameplay");
 public static string Status()=>log;
}
