using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using TMPro;
using RhythmRehab;
public static class UIInteractionFeedbackProbe
{
 static string status,phase;static int checks;static float began,stageStart,oldScale;
 static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static UIHoverFeedback hover;static Button button;static Vector3 originalScale;static Color originalColor;
 static PrototypeGameplay game;static ScorePulse pulse;static JudgementTextFeedback judgement;static TMP_Text feedback;
 static NoteHitEffects textParticles;static RectTransform feedbackRect;static Vector2 originalPosition;static bool sawScorePulse,sawRise,sawFade;
 static RhythmCalibrationController calibration;
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
 static object Field(object target,string name)=>GameplayEditorAccess.Read(target,name);
 public static string Main(){
  if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
  status="RUNNING";checks=0;oldScale=Time.timeScale;Time.timeScale=0;began=Time.realtimeSinceStartup;
  SessionWellbeing.Reset();phase="menu";IrisSceneTransition.Load("MainMenu");EditorApplication.update+=Tick;return "Checking hover, score pulse, judgement motion and particles at timeScale=0.";
 }
 static void Change(string next){phase=next;stageStart=Time.realtimeSinceStartup;}
 static void Tick(){
  try{
   if(Time.realtimeSinceStartup-began>30)throw new Exception("Timeout in "+phase);
   if(IrisSceneTransition.IsBusy)return;
   if(phase=="menu"){
    if(SceneManager.GetActiveScene().name!="MainMenu")return;
    button=GameObject.Find("/MenuCanvas/MenuButtons/StartButton").GetComponent<Button>();hover=button.GetComponent<UIHoverFeedback>();originalScale=button.transform.localScale;originalColor=button.targetGraphic.color;
    Check(hover,"Saved hover component exists");hover.OnPointerEnter(new PointerEventData(EventSystem.current));Change("hover");
   }else if(phase=="hover"&&Time.realtimeSinceStartup-stageStart>.25f){
    Check(hover.CurrentScale>1.02f,"Pointer hover grows button");Check(button.targetGraphic.color!=originalColor,"Pointer hover changes colour");hover.OnPointerExit(new PointerEventData(EventSystem.current));Change("exit");
   }else if(phase=="exit"&&Time.realtimeSinceStartup-stageStart>.25f){
    Check(Vector3.Distance(button.transform.localScale,originalScale)<.001f,"Hover exit restores scale");Check(button.targetGraphic.color==originalColor,"Hover exit restores colour");
    button.interactable=false;hover.OnPointerEnter(new PointerEventData(EventSystem.current));Change("disabled");
   }else if(phase=="disabled"&&Time.realtimeSinceStartup-stageStart>.25f){
    Check(hover.CurrentScale==1,"Disabled button does not grow");button.interactable=true;hover.OnPointerExit(new PointerEventData(EventSystem.current));IrisSceneTransition.Load("Gameplay");Change("game");
   }else if(phase=="game"){
    if(SceneManager.GetActiveScene().name!="Gameplay")return;game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(game.Summary==null)return;
    game.enabled=false;if(game.IsPaused)game.TogglePause();pulse=(ScorePulse)Field(game,"scorePulse");judgement=(JudgementTextFeedback)Field(game,"judgementMotion");feedback=(TMP_Text)Field(game,"feedbackLabel");feedbackRect=feedback.rectTransform;originalPosition=(Vector2)Field(judgement,"origin");textParticles=(NoteHitEffects)Field(judgement,"textEffects");
    var chart=(ChartNote[])Field(game,"chart");Array.Clear((bool[])Field(game,"resolved"),0,chart.Length);float offset=(float)Field(game,"judgementOffsetSeconds");
    int beforePulse=pulse.PulseCount,beforeParticles=textParticles.PlayedBursts;
    Check(game.TryHit(chart[0].beat*60.0/game.Summary.bpm+offset,chart[0].TargetHeight),"Perfect note consumed");
    Check(pulse.PulseCount==beforePulse+1,"Successful hit triggers score pulse");Check(textParticles.PlayedBursts==beforeParticles+2,"Successful judgement plays two coloured sparkles");
    Check(feedback.text=="PERFECT","Judgement text set");sawScorePulse=sawRise=sawFade=false;Change("motion");
   }else if(phase=="motion"){
    if(pulse.CurrentScale>1.05f)sawScorePulse=true;if(feedbackRect.anchoredPosition.y>originalPosition.y+5)sawRise=true;if(judgement.CurrentOpacity>.05f&&judgement.CurrentOpacity<.9f)sawFade=true;
    if(Time.realtimeSinceStartup-stageStart>.85f){
     Check(sawScorePulse,"Score visibly grows during animation");Check(Mathf.Abs(pulse.CurrentScale-1)<.001f,"Score returns to original size");Check(sawRise&&sawFade,"Judgement rises and fades over time");Check(!judgement.IsAnimating&&feedback.text=="","Judgement clears after fade");Check(feedbackRect.anchoredPosition==originalPosition,"Judgement resets without drifting");
     int beforePulse=pulse.PulseCount,beforeParticles=textParticles.PlayedBursts;var chart=(ChartNote[])Field(game,"chart");float offset=(float)Field(game,"judgementOffsetSeconds");
     game.TryHit(chart[1].beat*60.0/game.Summary.bpm+offset,chart[1].TargetHeight>.5f?0:1);
     Check(pulse.PulseCount==beforePulse,"Miss does not pulse score");Check(textParticles.PlayedBursts==beforeParticles,"Miss has no text particles");Check(feedback.text=="WRONG HEIGHT","Miss feedback remains visible");
     IrisSceneTransition.Load("RhythmCalibration");Change("calibration");
    }
   }else if(phase=="calibration"){
    if(SceneManager.GetActiveScene().name!="RhythmCalibration")return;calibration=UnityEngine.Object.FindAnyObjectByType<RhythmCalibrationController>();calibration.enabled=false;calibration.ResetValues();calibration.TogglePreview();
    var motion=(JudgementTextFeedback)Field(calibration,"judgementMotion");var particles=(NoteHitEffects)Field(motion,"textEffects");int before=particles.PlayedBursts;
    Check(calibration.TryHit(2,0)&&motion.IsAnimating&&particles.PlayedBursts==before+2,"Calibration uses animated judgement and particles");
    calibration.StopPreview();Check(!motion.IsAnimating&&GameObject.Find("/Canvas/PreviewPanel/Feedback").GetComponent<TMP_Text>().text=="PREVIEW STOPPED","Stopping preview restores idle text");
    status="PASS "+checks+" live hover, score, fade, reset and miss checks; animations work with paused game time. No sessions or settings saved.";End();
   }
  }catch(Exception error){status="FAIL "+error.Message+" ("+phase+")";End();}
 }
 static void End(){EditorApplication.update-=Tick;Time.timeScale=oldScale;}
 public static string Status()=>status;
}
