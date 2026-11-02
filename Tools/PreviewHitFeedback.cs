using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RhythmRehab;
public static class PreviewHitFeedback
{
 public static string Main(){
  var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(!game||game.Summary==null||IrisSceneTransition.IsBusy)throw new Exception("Wait for Gameplay.");game.enabled=false;if(game.IsPaused)game.TogglePause();
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;var chart=(ChartNote[])GameplayEditorAccess.Read(game,"chart");Array.Clear((bool[])GameplayEditorAccess.Read(game,"resolved"),0,chart.Length);
  var motion=(JudgementTextFeedback)GameplayEditorAccess.Read(game,"judgementMotion");motion.enabled=false;
  float offset=(float)GameplayEditorAccess.Read(game,"judgementOffsetSeconds");game.TryHit(chart[0].beat*60.0/game.Summary.bpm+offset,chart[0].TargetHeight);
  typeof(JudgementTextFeedback).GetField("elapsed",flags).SetValue(motion,.27f);typeof(JudgementTextFeedback).GetMethod("LateUpdate",flags).Invoke(motion,null);
  var pulse=(ScorePulse)GameplayEditorAccess.Read(game,"scorePulse");pulse.enabled=false;typeof(ScorePulse).GetField("running",flags).SetValue(pulse,true);typeof(ScorePulse).GetField("elapsed",flags).SetValue(pulse,.065f);typeof(ScorePulse).GetMethod("LateUpdate",flags).Invoke(pulse,null);
  foreach(var root in game.gameObject.scene.GetRootGameObjects()){
   foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))if(ps.isPlaying){ps.Simulate(.08f,false,false);ps.Pause(false);}
   foreach(var graphic in root.GetComponentsInChildren<CanvasParticleGraphic>(true))graphic.SendMessage("LateUpdate");
  }
  Canvas.ForceUpdateCanvases();return "Frozen visual sample of score pulse, floating judgement and coloured sparkles; no history saved.";
 }
}
