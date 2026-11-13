using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RhythmRehab;
public static class VerifyUsbGameplayLive
{
 static int phase,checks;static double began;static UsbSensorConnection sensor;static UsbImuTransport feed;static PrototypeGameplay game;
 static FieldInfo Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 static void Set(object o,string name,object value)=>Field(o,name).SetValue(o,value);
 static void Feed(float angle)
 {
  var q=Quaternion.AngleAxis(angle,Vector3.right);Set(feed,"latest",new ImuSerialPacket((ushort)checks,q));Set(feed,"receivedAt",UsbImuTransport.Now);Set(feed,"version",(long)(checks+1));Set(feed,"open",true);
 }
 public static string Main()
 {
  if(!EditorApplication.isPlaying||IrisSceneTransition.IsBusy)throw new Exception("Start SensorConnection Play mode first.");
  sensor=UsbSensorConnection.Instance;feed=new UsbImuTransport("TEST-NO-PORT-OPENED");Set(sensor,"transport",feed);Feed(0);
  sensor.Calibration.CaptureRest(Vector3.forward);Check(sensor.Calibration.CaptureRaised(new ImuSerialPacket(0,Quaternion.AngleAxis(70,Vector3.right)).Gravity,out _),"Valid calibration");
  began=EditorApplication.timeSinceStartup;phase=0;checks=1;EditorApplication.update+=Tick;
  return "Checking gameplay with simulated USB samples; no COM port opened and no patient session saved.";
 }
 static void Tick()
 {
  try
  {
   if(EditorApplication.timeSinceStartup-began>35)throw new Exception("USB gameplay checks timed out.");
   if(phase<4)Feed(35);
   if(IrisSceneTransition.IsBusy)return;
   if(phase==0)
   {
    if(!sensor.HasFreshReading)return;sensor.UseSensor();Check(sensor.InputEnabled,"Sensor height enabled after live sample and calibration");
    var catalog=AssetDatabase.LoadAssetAtPath<SongCatalog>("Assets/Data/ClassicalSongs.asset");SessionSelection.SelectSong(catalog.songs[0]);SessionSelection.SelectMode(SessionMode.Free);SessionSelection.SelectDifficulty(FreeDifficulty.Medium);SessionSelection.Confirm();
    IrisSceneTransition.Load("Gameplay");phase=1;
   }
   else if(phase==1)
   {
    game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(!game||game.IsStarting)return;
    if(game.IsPaused)game.TogglePause();began=EditorApplication.timeSinceStartup;phase=2;
   }
   else if(phase==2)
   {
    if(EditorApplication.timeSinceStartup-began<.4)return;
    Check(Mathf.Abs(game.CurrentHeight-.5f)<.02,"Gameplay reads calibrated USB height instead of mouse");
    Check(Mathf.Abs(game.Playfield.Marker.anchoredPosition.y-game.Playfield.PositionAt(.5f).y)<15,"White marker follows USB height");
    var tracker=UnityEngine.Object.FindAnyObjectByType<HumanLegTracker>();Check(tracker&&Mathf.Abs(tracker.DisplayedHeight-.5f)<.02,"3D leg follows USB height");
    Check(game.Summary.sensorHeightControl,"Session records sensor input source");
    var label=(TMPro.TMP_Text)GameplayEditorAccess.Read(game.Hud,"statusLabel");Check(label.text.Contains("SHIN SENSOR"),"Gameplay controls describe sensor input");
    Set(feed,"receivedAt",UsbImuTransport.Now-2);phase=4;began=EditorApplication.timeSinceStartup;
   }
   else if(phase==4)
   {
    if(EditorApplication.timeSinceStartup-began<.2)return;
    Check(game.IsPaused,"Stale readings pause gameplay");Check(Mathf.Abs(game.CurrentHeight-.5f)<.02,"Signal loss does not fall back to mouse");
    var label=(TMPro.TMP_Text)GameplayEditorAccess.Read(game.Hud,"statusLabel");Check(label.text.Contains("SENSOR DATA LOST"),"Signal-loss feedback shown");
    phase=5;Feed(35);began=EditorApplication.timeSinceStartup;
   }
   else if(phase==5)
   {
    Feed(35);if(EditorApplication.timeSinceStartup-began<.2)return;
    Check(game.IsPaused,"Returning data does not automatically resume");
    game.Home();sensor.Disconnect();phase=6;
   }
   else if(phase==6)
   {
    Check(!sensor.InputEnabled&&!sensor.Calibration.IsReady,"Disconnect clears references");
    File.WriteAllText("Temp/usb-gameplay-result.txt","PASS "+checks+" live integration checks using simulated samples: height, marker, 3D model, source recording, stale-data pause and manual resume. No hardware or history writes.");
    EditorApplication.update-=Tick;
   }
  }
  catch(Exception e)
  {
   if(game)GameplayEditorAccess.Write(game,"finished",true);if(sensor)sensor.Disconnect();
   File.WriteAllText("Temp/usb-gameplay-result.txt","FAIL: "+e);EditorApplication.update-=Tick;Debug.LogError(e);
  }
 }
}
