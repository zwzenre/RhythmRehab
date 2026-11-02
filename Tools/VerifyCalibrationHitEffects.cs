using System;
using System.Reflection;
using UnityEngine;
using RhythmRehab;
public static class VerifyCalibrationHitEffects
{
 public static string Main(){
  var controller=UnityEngine.Object.FindAnyObjectByType<RhythmCalibrationController>();
  if(IrisSceneTransition.IsBusy)throw new Exception("Wait for transition.");
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  var fx=(NoteHitEffects)typeof(RhythmCalibrationController).GetField("hitEffects",flags).GetValue(controller);
  if(!fx)throw new Exception("Missing saved preview effect reference.");
  int checks=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);checks++;};
  controller.enabled=false;if(controller.IsPlaying)controller.StopPreview();controller.ResetValues();controller.TogglePreview();
  check(controller.IsPlaying,"Preview starts");
  int before=fx.PlayedBursts;var positions=(RectTransform[])typeof(NoteHitEffects).GetField("burstPositions",flags).GetValue(fx);
  double[] times={2,3.10,4.18};
  for(int i=0;i<3;i++){
   check(controller.TryHit(times[i],i*.5f),"Successful preview hit "+i);
   check(fx.PlayedBursts==before+i+1,"Preview effect plays "+i);
   check(positions[i].anchoredPosition==new Vector2(-350,Mathf.Lerp(-110,110,i*.5f)),"Effect at actual note height "+i);
   foreach(var graphic in positions[i].GetComponentsInChildren<CanvasParticleGraphic>())check(graphic.color==fx.ColorFor((HitJudgement)i),"Judgement colour "+i);
  }
  controller.TryHit(5,1);check(fx.PlayedBursts==before+3,"Wrong-height miss has no effect");
  controller.TryHit(-100,0);check(fx.PlayedBursts==before+3,"Out-of-window hit has no effect");
  controller.StopPreview();int alive=0;foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>())alive+=ps.particleCount;
  check(alive==0,"Stop clears preview effects");
  controller.TogglePreview();for(int i=0;i<3;i++)controller.TryHit(times[i],i*.5f);
  foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>())if(ps.isPlaying){ps.Simulate(.07f,false,false);ps.Pause(false);}
  foreach(var graphic in fx.GetComponentsInChildren<CanvasParticleGraphic>())graphic.SendMessage("LateUpdate");Canvas.ForceUpdateCanvases();
  int vertices=0;foreach(var graphic in fx.GetComponentsInChildren<CanvasParticleGraphic>()){var mesh=graphic.canvasRenderer.GetMesh();if(mesh)vertices+=mesh.vertexCount;}
  check(vertices>0,"Preview particles rendered through Canvas");
  return checks+" calibration effect checks passed; "+vertices+" visible vertices. No settings or history saved.";
 }
}
