using System;
using System.Reflection;
using UnityEngine;
using RhythmRehab;
public static class VerifyNoteHitEffects
{
 public static string Main(){
  var g=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();var fx=(NoteHitEffects)GameplayEditorAccess.Read(g,"hitEffects");
  if(!EditorReady(g))throw new Exception("Gameplay has not started.");
  g.enabled=false;if(g.IsPaused)g.TogglePause();
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;var chart=(ChartNote[])GameplayEditorAccess.Read(g,"chart");
  var resolved=(bool[])GameplayEditorAccess.Read(g,"resolved");Array.Clear(resolved,0,resolved.Length);
  float offset=(float)GameplayEditorAccess.Read(g,"judgementOffsetSeconds");
  double scale=(double)GameplayEditorAccess.Read(g,"timingScale");
  int checks=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);checks++;};
  int before=fx.PlayedBursts;
  double[] errors={0,.10*scale,.18*scale};
  for(int i=0;i<3;i++){
   check(g.TryHit(chart[i].beat*60.0/g.Summary.bpm+offset+errors[i],chart[i].TargetHeight),"Successful hit "+i);
   check(fx.PlayedBursts==before+i+1,"Effect on Perfect/Great/Good "+i);
   var positions=(RectTransform[])typeof(NoteHitEffects).GetField("burstPositions",flags).GetValue(fx);
   var expected=fx.ColorFor((HitJudgement)i);
   foreach(var graphic in positions[i].GetComponentsInChildren<CanvasParticleGraphic>())
    check(graphic.color==expected,"Judgement colour applied to all click layers "+i);
  }
  check(g.Summary.perfect>=1&&g.Summary.great>=1&&g.Summary.good>=1,"All three success judgements exercised");
  g.TryHit(chart[3].beat*60.0/g.Summary.bpm+offset,chart[3].TargetHeight>.5f?0:1);
  check(fx.PlayedBursts==before+3,"Wrong-height miss has no burst");
  GameplayEditorAccess.Invoke(g,"Resolve",new object[]{4,HitJudgement.Miss,"Miss",true});
  check(fx.PlayedBursts==before+3,"Automatic miss has no burst");
  check(!g.TryHit(-100,0)&&fx.PlayedBursts==before+3,"Empty Space press has no burst");
  fx.Play(Vector2.zero,HitJudgement.Miss);
  check(fx.PlayedBursts==before+3,"Effect controller rejects Miss directly");
  fx.Clear();fx.Play(new Vector2(-720,0));
  int particles=0;
  foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>()){
   if(ps.isPlaying){ps.Simulate(.09f,false,false);ps.Pause(false);}particles+=ps.particleCount;
  }
  foreach(var graphic in fx.GetComponentsInChildren<CanvasParticleGraphic>())graphic.SendMessage("LateUpdate");
  Canvas.ForceUpdateCanvases();
  check(particles>0,"Package burst emits particles");
  int vertices=0;foreach(var graphic in fx.GetComponentsInChildren<CanvasParticleGraphic>()){
   var mesh=graphic.canvasRenderer.GetMesh();if(mesh)vertices+=mesh.vertexCount;
  }
  check(vertices>0,"Canvas renders visible particle geometry");
  return checks+" hit-effect checks passed; live particles="+particles+", Canvas vertices="+vertices+". No sessions saved.";
 }
 private static bool EditorReady(PrototypeGameplay g)=>g&&g.Summary!=null&&!IrisSceneTransition.IsBusy;
}
