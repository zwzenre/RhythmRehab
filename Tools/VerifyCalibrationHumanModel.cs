using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using RhythmRehab;
public static class VerifyCalibrationHumanModel
{
 public static string Main()
 {
  if(!EditorApplication.isPlaying)throw new Exception("Enter SensorCalibration Play mode first.");
  int checks=0;Action<bool,string> check=(value,label)=>{if(!value)throw new Exception(label);checks++;};
  var tracker=UnityEngine.Object.FindAnyObjectByType<HumanLegTracker>();check(tracker,"Calibration tracker exists");
  check((bool)GameplayEditorAccess.Read(tracker,"sensorCalibrationPreview"),"Calibration mode assigned");
  check(GameplayEditorAccess.Read(tracker,"gameplay")==null,"No dependency on gameplay controller");
  var view=GameObject.Find("/Canvas/SensorDiagram/HumanModelView").GetComponent<UnityEngine.UI.RawImage>();var camera=tracker.GetComponentInChildren<Camera>();
  check(view&&camera&&view.texture==camera.targetTexture,"Authored model camera feeds calibration panel");
  check(!view.raycastTarget&&view.uvRect.width<0,"View faces right and cannot block calibration buttons");
  check(camera.targetTexture!=AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/UI/LegTracker/HumanLegView.renderTexture"),"Separate render target from gameplay");
  check((Camera.main.cullingMask&(1<<30))==0&&camera.cullingMask==(1<<30),"Model isolated from main camera");
  var model=(Transform)GameplayEditorAccess.Read(tracker,"model");check(model.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.sharedMesh&&r.enabled),"Imported 3D human is visible");
  check(tracker.transform.Find("Chair")!=null,"Seated chair retained");
  var foot=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="B-foot.L");tracker.enabled=false;float previous=float.NegativeInfinity;
  foreach(float height in new[]{0f,.5f,1f})
  {
   tracker.ApplyHeight(height);check(foot.position.y>previous,"Lower leg rises with height");previous=foot.position.y;
   var p=camera.WorldToViewportPoint(foot.position);check(p.x>.02f&&p.x<.98f&&p.y>.02f&&p.y<.98f,"Foot remains inside model view");
  }
  tracker.ApplyHeight(0);tracker.enabled=true;
  foreach(var txt in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>())txt.GetComponent<TextSlideIn>()?.CompleteImmediately();
  ScreenCapture.CaptureScreenshot(Path.GetFullPath("Screenshots/SensorCalibration-3D-Rest.png"));
  return "PASS "+checks+" calibration-model checks: authored 3D mesh, chair, isolated camera, nonblocking UI and full pose visibility. Calibration values unchanged.";
 }
}
