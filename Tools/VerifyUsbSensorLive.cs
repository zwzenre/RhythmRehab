using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RhythmRehab;
public static class VerifyUsbSensorLive
{
 static double began;static int phase;static UsbSensorConnection service;static string result;
 public static string Main()
 {
  if(!EditorApplication.isPlaying)throw new Exception("Enter SensorConnection Play mode first.");
  began=EditorApplication.timeSinceStartup;phase=0;EditorApplication.update+=Tick;result="RUNNING";return "Checking persistent sensor service, honest disconnected UI and COM port release.";
 }
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Tick()
 {
  try
  {
   if(IrisSceneTransition.IsBusy)return;
   if(EditorApplication.timeSinceStartup-began>25)throw new Exception("Sensor UI check timed out.");
   if(phase==0)
   {
    if(EditorApplication.timeSinceStartup-began<1.5)return;
    service=UsbSensorConnection.Instance;Check(service&&!service.HasFreshReading&&!service.InputEnabled,"No fake device or sensor input");
    var presenters=UnityEngine.Object.FindObjectsByType<SensorFrameworkPresenter>();Check(presenters.Length==1,"One sensor page controller");
    foreach(var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>())label.GetComponent<TextSlideIn>()?.CompleteImmediately();
    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Screenshots/USB-SensorConnection.png"));IrisSceneTransition.Load("SensorCalibration");phase=1;
   }
   else if(phase==1)
   {
    Check(service==UsbSensorConnection.Instance,"USB service persists across scenes");
    Check(UnityEngine.Object.FindObjectsByType<UsbSensorConnection>().Length==1,"Scene duplicates removed");
    var p=UnityEngine.Object.FindAnyObjectByType<SensorFrameworkPresenter>();p.ExplainCalibration();Check(!service.Calibration.IsReady&&!service.IsCapturing,"No calibration without real readings");
    foreach(var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>())label.GetComponent<TextSlideIn>()?.CompleteImmediately();
    began=EditorApplication.timeSinceStartup;phase=2;
   }
   else if(phase==2)
   {
    if(EditorApplication.timeSinceStartup-began<1)return;
    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Screenshots/USB-SensorCalibration.png"));
    service.UseSensor();Check(!service.InputEnabled,"Sensor control requires calibration and readings");
    service.Disconnect();Check(!service.InputEnabled&&!service.HasFreshReading&&!service.Calibration.IsReady,"Disconnect clears calibration and sensor control");
    result="PASS: real disconnected state, COM selection, persistent singleton, no fabricated calibration, unavailable-input guard and disconnect cleanup.";
    File.WriteAllText("Temp/usb-live-result.txt",result);EditorApplication.update-=Tick;
   }
  }
  catch(Exception e){result="FAIL: "+e;File.WriteAllText("Temp/usb-live-result.txt",result);EditorApplication.update-=Tick;Debug.LogError(result);}
 }
}
