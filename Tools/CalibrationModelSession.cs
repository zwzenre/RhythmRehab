using System;
using UnityEngine;
using UnityEditor;
using RhythmRehab;
public static class CalibrationModelSession
{
 const string Key="RhythmRehab.CalibrationModel.";
 public static string Capture()
 {
  var s=UsbSensorConnection.Instance;SessionState.SetBool(Key+"valid",s&&s.IsOpen);
  if(!s||!s.IsOpen)return "No active USB connection to preserve.";
  if(s.IsCapturing)throw new Exception("Wait for the position capture to finish before updating the scene.");
  SessionState.SetString(Key+"port",s.PortName);SessionState.SetBool(Key+"rest",s.Calibration.HasRest);SessionState.SetBool(Key+"ready",s.Calibration.IsReady);SessionState.SetBool(Key+"input",s.InputEnabled);
  SessionState.SetString(Key+"restVector",JsonUtility.ToJson((Vector3)GameplayEditorAccess.Read(s.Calibration,"rest")));
  SessionState.SetString(Key+"axis",JsonUtility.ToJson((Vector3)GameplayEditorAccess.Read(s.Calibration,"axis")));
  SessionState.SetFloat(Key+"range",s.Calibration.RangeDegrees);
  var transport=(UsbImuTransport)GameplayEditorAccess.Read(s,"transport");transport.Snapshot(out var packet,out _,out _);SessionState.SetInt(Key+"sequence",packet.sequence);
  return "Preserved USB port and captured calibration references for the model-only scene update.";
 }
 public static string Reconnect()
 {
  if(!SessionState.GetBool(Key+"valid",false))return "No USB connection was active before this edit.";
  var s=UsbSensorConnection.Instance;if(!s)throw new Exception("Wait for the sensor scene to start.");
  s.Connect(SessionState.GetString(Key+"port",""));return "Reopening the previous USB port.";
 }
 public static string Restore()
 {
  if(!SessionState.GetBool(Key+"valid",false))return "No calibration to restore.";
  var s=UsbSensorConnection.Instance;if(!s||!s.HasFreshReading)throw new Exception("Wait for fresh USB readings before restoring the reference.");
  var transport=(UsbImuTransport)GameplayEditorAccess.Read(s,"transport");transport.Snapshot(out var packet,out _,out _);
  int forward=(packet.sequence-SessionState.GetInt(Key+"sequence",0)+65536)%65536;
  if(forward>40000)throw new Exception("The sensor may have restarted. Capture resting and raised positions again.");
  if(SessionState.GetBool(Key+"rest",false))
  {
   var rest=JsonUtility.FromJson<Vector3>(SessionState.GetString(Key+"restVector","{}"));s.Calibration.CaptureRest(rest);
   if(SessionState.GetBool(Key+"ready",false))
   {
    var axis=JsonUtility.FromJson<Vector3>(SessionState.GetString(Key+"axis","{}"));var raised=Quaternion.AngleAxis(SessionState.GetFloat(Key+"range",0),axis)*rest;
    if(!s.Calibration.CaptureRaised(raised,out var error))throw new Exception(error);
    if(SessionState.GetBool(Key+"input",false))s.UseSensor();
   }
   if(!s.Calibration.IsReady)GameplayEditorAccess.Write(s,"message","Rest position captured. Raise your lower leg to your chosen upper position, hold still, then capture raised.");
  }
  SessionState.SetBool(Key+"valid",false);return "USB restored; Rest="+s.Calibration.HasRest+" Raised="+s.Calibration.IsReady+". This restores only the reference captured before the visual edit.";
 }
}
