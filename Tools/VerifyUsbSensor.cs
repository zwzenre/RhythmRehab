using System;
using System.Globalization;
using System.Threading;
using UnityEngine;
using RhythmRehab;
public static class VerifyUsbSensor
{
 static int checks;
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
 static string Frame(ushort seq,Quaternion q)=>string.Format(CultureInfo.InvariantCulture,"IMU,GAIT-SHANK,{0},{1:R},{2:R},{3:R},{4:R}",seq,q.w,q.x,q.y,q.z);
 static ImuSerialPacket Packet(Quaternion q){Check(ImuSerialPacket.TryParse(Frame(7,q),out var p),"Valid quaternion");return p;}
 public static string Main()
 {
  checks=0;
  Check(ImuSerialPacket.TryParse("IMU,GAIT-SHANK,65535,1,0,0,0\r\n",out var first)&&first.sequence==65535,"CRLF and max sequence");
  Check(ImuSerialPacket.TryParse("IMU,GAIT-SHANK,0,1,0,0,0",out var wrapped)&&wrapped.sequence==0,"Sequence rollover");
  foreach(var bad in new[]{"# Keep sensor still","GAIT-FOOT ready","IMU,GAIT-FOOT,1,1,0,0,0","IMU,GAIT-SHANK,65536,1,0,0,0","IMU,GAIT-SHANK,1,0,0,0,0","IMU,GAIT-SHANK,1,NaN,0,0,0","IMU,GAIT-SHANK,1,Infinity,0,0,0","IMU,GAIT-SHANK,1,2,0,0,0","IMU,GAIT-SHANK,1,1,0,0","IMU,GAIT-SHANK,1,1,0,0,0,extra",new string('x',200)})
   Check(!ImuSerialPacket.TryParse(bad,out _),"Reject invalid packet: "+bad.Substring(0,Math.Min(30,bad.Length)));
  var old=Thread.CurrentThread.CurrentCulture;try{Thread.CurrentThread.CurrentCulture=new CultureInfo("fr-FR");Check(ImuSerialPacket.TryParse("IMU,GAIT-SHANK,1,0.9238795,0.3826834,0,0",out _),"Invariant decimals");}finally{Thread.CurrentThread.CurrentCulture=old;}
  foreach(var mount in new[]{Quaternion.identity,Quaternion.Euler(25,73,16),Quaternion.Euler(-52,0,144)})
  {
   var rest=Packet(mount);var raised=Packet(Quaternion.AngleAxis(70,Vector3.right)*mount);var calibration=new ShinHeightCalibration();
   Check(!calibration.CaptureRaised(raised.Gravity,out _),"Rest required");calibration.CaptureRest(rest.Gravity);
   Check(calibration.CaptureRaised(raised.Gravity,out _),"Capture learned movement axis");
   Check(Mathf.Abs(calibration.Height(rest.Gravity))<.001,"Rest is zero");Check(Mathf.Abs(calibration.Height(raised.Gravity)-1)<.001,"Raised is one");
   var half=Packet(Quaternion.AngleAxis(35,Vector3.right)*mount);Check(Mathf.Abs(calibration.Height(half.Gravity)-.5f)<.001,"Halfway maps to 50 percent");
   var yaw=Packet(Quaternion.AngleAxis(125,Vector3.forward)*Quaternion.AngleAxis(35,Vector3.right)*mount);
   Check(Mathf.Abs(calibration.Height(yaw.Gravity)-.5f)<.001,"Global yaw drift does not change height");
   var beyond=Packet(Quaternion.AngleAxis(85,Vector3.right)*mount);Check(calibration.Height(beyond.Gravity)==1,"Upper clamp");
   var below=Packet(Quaternion.AngleAxis(-10,Vector3.right)*mount);Check(calibration.Height(below.Gravity)==0,"Lower clamp");
   calibration.Reset();Check(!calibration.IsReady&&!calibration.HasRest,"Reset invalidates reference");
  }
  var c=new ShinHeightCalibration();c.CaptureRest(Vector3.forward);Check(!c.CaptureRaised(Vector3.forward,out _),"Identical reference rejected");Check(!c.CaptureRaised(Quaternion.AngleAxis(5,Vector3.right)*Vector3.forward,out _),"Tiny movement rejected");Check(!c.CaptureRaised(-Vector3.forward,out _),"Ambiguous opposite reference rejected");
  Check(ImuSerialPacket.TryParse("IMU,GAIT-SHANK,1,-1,0,0,0",out var negative)&&Vector3.Distance(negative.Gravity,first.Gravity)<.001,"Quaternion sign equivalent");
  Check(ImuSerialPacket.TryParse("IMU,GAIT-SHANK,1,0.98,0,0,0",out var normalized)&&Mathf.Abs(normalized.orientation.w-1)<.001,"Quaternion normalized");
  var ports=UsbImuTransport.Ports();Check(ports!=null,"COM enumeration supported by Unity");
  var source=new SessionSummary{sensorHeightControl=true};Check(JsonUtility.FromJson<SessionSummary>(JsonUtility.ToJson(source)).sensorHeightControl,"Sensor input source survives JSON");
  Check(!JsonUtility.FromJson<SessionSummary>("{\"songTitle\":\"Legacy session\"}").sensorHeightControl,"Legacy history defaults to mouse input");
  return "PASS "+checks+" checks: USB CSV validation, rollover, non-English locale, mount-independent height calibration, yaw rejection, limits and COM enumeration. Available ports: "+string.Join(", ",ports);
 }
}
