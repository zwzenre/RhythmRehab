using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using TMPro;
using RhythmRehab;
public static class BackgroundMotionProbe
{
 static string status,phase;static int index,checks;static float began,sampledAt,oldScale;
 static readonly string[] scenes={"MainMenu","Settings","Gameplay"};
 static RawImage backdrop;static RectTransform cloud,title,character;static Rect oldUV;static Vector2 cloudPosition,titlePosition,characterPosition;
 static GentleBackgroundMotion textureMotion,cloudMotion;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
 public static string Main(){
  if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");status="RUNNING";checks=index=0;oldScale=Time.timeScale;Time.timeScale=0;began=Time.realtimeSinceStartup;EditorApplication.update+=Tick;Next();return "Checking background drift, fixed UI, edge coverage and Gameplay exclusion at paused game time.";
 }
 static void Next(){phase="load";IrisSceneTransition.Load(scenes[index]);}
 static void Tick(){
  try{
   if(Time.realtimeSinceStartup-began>25)throw new Exception("Motion test timeout");
   if(IrisSceneTransition.IsBusy||SceneManager.GetActiveScene().name!=scenes[index])return;
   var scene=SceneManager.GetActiveScene();
   if(phase=="load"){
    var roots=scene.GetRootGameObjects();var motions=roots.SelectMany(r=>r.GetComponentsInChildren<GentleBackgroundMotion>(true)).ToArray();
    if(scene.name=="Gameplay"){
     Check(motions.Length==0,"Gameplay background unchanged");var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(game)game.enabled=false;
     status="PASS "+checks+" live movement, reset and coverage checks; menu/settings backgrounds move while UI stays fixed, including timeScale=0. Gameplay excluded.";End();return;
    }
    Check(motions.Length>=28,"Saved moving background and decoration layers in "+scene.name);
    backdrop=roots.SelectMany(r=>r.GetComponentsInChildren<RawImage>(true)).First(i=>i.name=="SpaceBackdrop");textureMotion=backdrop.GetComponent<GentleBackgroundMotion>();
    cloud=roots.SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true)).First(r=>r.name=="CloudLeft");cloudMotion=cloud.GetComponent<GentleBackgroundMotion>();
    title=roots.SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true)).First(t=>t.name==(scene.name=="MainMenu"?"GameTitle":"Title")).rectTransform;
    var person=roots.SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true)).FirstOrDefault(r=>r.name=="GuyLiftingLeg");character=person;
    oldUV=backdrop.uvRect;cloudPosition=cloud.anchoredPosition;titlePosition=title.anchoredPosition;if(character)characterPosition=character.anchoredPosition;sampledAt=Time.realtimeSinceStartup;phase="sample";
   }else if(Time.realtimeSinceStartup-sampledAt>2.2f){
    Check(backdrop.uvRect!=oldUV,"Space texture drifts over time");Check(Vector2.Distance(cloud.anchoredPosition,cloudPosition)>5,"Cloud movement is clearly visible");
    Check(title.anchoredPosition==titlePosition,"Foreground title stays fixed");if(character)Check(character.anchoredPosition==characterPosition,"Main menu character stays fixed");
    var uv=backdrop.uvRect;Check(uv.xMin>=0&&uv.yMin>=0&&uv.xMax<=1&&uv.yMax<=1,"Drift stays inside texture edges");
    Check(!backdrop.raycastTarget,"Background cannot block controls");
    var f=BindingFlags.Instance|BindingFlags.NonPublic;var original=(Rect)typeof(GentleBackgroundMotion).GetField("originalUV",f).GetValue(textureMotion);var origin=(Vector2)typeof(GentleBackgroundMotion).GetField("origin",f).GetValue(cloudMotion);
    textureMotion.enabled=false;cloudMotion.enabled=false;Check(backdrop.uvRect==original&&cloud.anchoredPosition==origin,"Disabling motion restores saved appearance");textureMotion.enabled=true;cloudMotion.enabled=true;
    index++;Next();
   }
  }catch(Exception ex){status="FAIL "+ex.Message;End();}
 }
 static void End(){EditorApplication.update-=Tick;Time.timeScale=oldScale;}
 public static string Status()=>status;
}
