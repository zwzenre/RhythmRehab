using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class AuthorNoteHitEffects
{
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before authoring.");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.name!="Gameplay")throw new Exception("Open Gameplay first.");
  var gameplay=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();
  var serialized=new SerializedObject(gameplay);
  var field=(RectTransform)GameplayEditorAccess.Owner(gameplay,"playfield").FindProperty("playfield").objectReferenceValue;
  if(field.Find("NoteHitEffects"))throw new Exception("Effects already authored; edit the existing pool.");
  var root=new GameObject("NoteHitEffects",typeof(RectTransform),typeof(NoteHitEffects));
  var rect=root.GetComponent<RectTransform>();rect.SetParent(field,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=Vector2.zero;
  var positions=new List<RectTransform>();var systems=new List<ParticleSystem>();
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/BoldVFXPackDemo/Content/URP/Prefabs/FX_Stars_Yellow.prefab");
  for(int i=0;i<4;i++){
   var slot=new GameObject("HitBurst"+i,typeof(RectTransform));var position=slot.GetComponent<RectTransform>();position.SetParent(rect,false);position.sizeDelta=Vector2.zero;
   var effect=(GameObject)PrefabUtility.InstantiatePrefab(prefab,slot.transform);effect.transform.localPosition=Vector3.zero;effect.transform.localRotation=Quaternion.identity;effect.transform.localScale=Vector3.one;
   var parent=effect.GetComponent<ParticleSystem>();positions.Add(position);systems.Add(parent);
   foreach(var ps in effect.GetComponentsInChildren<ParticleSystem>(true)){
    ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    var main=ps.main;main.loop=false;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;main.maxParticles=128;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;main.simulationSpace=ParticleSystemSimulationSpace.Local;
    var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.enabled=false;
    var sheet=ps.textureSheetAnimation;
    if(!sheet.enabled||sheet.mode!=ParticleSystemAnimationMode.Sprites||sheet.spriteCount==0)continue;
    var graphicGo=new GameObject(ps.name+"Canvas",typeof(RectTransform),typeof(CanvasRenderer),typeof(CanvasParticleGraphic));var gr=graphicGo.GetComponent<RectTransform>();gr.SetParent(position,false);gr.sizeDelta=new Vector2(220,220);gr.anchoredPosition=Vector2.zero;
    var graphic=graphicGo.GetComponent<CanvasParticleGraphic>();graphic.raycastTarget=false;
    var so=new SerializedObject(graphic);so.FindProperty("source").objectReferenceValue=ps;so.FindProperty("particleSprite").objectReferenceValue=sheet.GetSprite(0);so.FindProperty("pixelsPerUnit").floatValue=35;so.ApplyModifiedPropertiesWithoutUndo();
   }
  }
  var controller=root.GetComponent<NoteHitEffects>();var config=new SerializedObject(controller);
  var p=config.FindProperty("burstPositions");var b=config.FindProperty("bursts");p.arraySize=b.arraySize=4;
  for(int i=0;i<4;i++){p.GetArrayElementAtIndex(i).objectReferenceValue=positions[i];b.GetArrayElementAtIndex(i).objectReferenceValue=systems[i];}config.ApplyModifiedPropertiesWithoutUndo();
  serialized.FindProperty("hitEffects").objectReferenceValue=controller;serialized.ApplyModifiedPropertiesWithoutUndo();
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  Selection.activeGameObject=root;
  return "Saved four reusable package star bursts with 12 Canvas particle renderers; gameplay success callback wired.";
 }
}
