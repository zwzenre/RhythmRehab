using System;
using System.Reflection;
using UnityEngine;
using RhythmRehab;
public static class VerifyContinuousHeightsLive
{
 public static string Main()
 {
  var g=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();g.enabled=false;if(g.IsPaused)g.TogglePause();
  const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
  var type=typeof(PrototypeGameplay);var chart=(ChartNote[])GameplayEditorAccess.Read(g,"chart");
  var resolved=(bool[])GameplayEditorAccess.Read(g,"resolved");
  Array.Clear(resolved,0,resolved.Length);
  var s=g.Summary;s.perfect=s.great=s.good=s.miss=s.score=s.heightAttempts=s.correctHeightAttempts=0;
  int index=Array.FindIndex(chart,n=>n.TargetHeight==.25f);
  if(index<0)throw new Exception("No intermediate note to test.");
  int checks=0;
  Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);checks++;};
  var n=chart[index];
  check(g.TryHit(n.beat*60.0/s.bpm,n.TargetHeight)&&s.perfect==1,"Exact intermediate target must score Perfect");
  check(s.correctHeightAttempts==1,"Intermediate height-match metric");
  resolved[index]=false;
  float wrong=n.TargetHeight<.5f?1:0;
  check(g.TryHit(n.beat*60.0/s.bpm,wrong)&&s.miss==1,"Wrong intermediate height must miss");
  int upper=Array.FindIndex(chart,note=>note.TargetHeight==.75f);
  if(upper>=0)check(g.TryHit(chart[upper].beat*60.0/s.bpm,.75f)&&s.perfect==2,"Exact halfway MID–HIGH hit");
  Array.Clear(resolved,0,resolved.Length);
  GameplayEditorAccess.Write(g,"startDsp",AudioSettings.dspTime-5);
  GameplayEditorAccess.Invoke(g,"Update",null); // Freeze a real rendered chart frame for inspection.
  var views=(RectTransform[])GameplayEditorAccess.Read(g,"noteViews");
  float spacing=(float)GameplayEditorAccess.Read(g,"laneSpacing");int visible=0;
  for(int i=0;i<chart.Length;i++)if(views[i].gameObject.activeSelf)
  {
   check(Math.Abs(views[i].anchoredPosition.y-Mathf.Lerp(-spacing,spacing,chart[i].TargetHeight))<.01f,"Rendered height equals hit target");visible++;
  }
  check(visible>0,"Visible intermediate chart notes");
  var label=(TMPro.TMP_Text)GameplayEditorAccess.Read(g,"heightLabel");
  check(label.text.StartsWith("HEIGHT"),"Free marker shows continuous height");
  return checks+" live rendering, intermediate-hit, wrong-height and marker checks passed. No session saved.";
 }
}
