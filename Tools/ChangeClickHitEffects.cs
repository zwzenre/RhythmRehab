using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class ChangeClickHitEffects
{
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var fx=UnityEngine.Object.FindAnyObjectByType<NoteHitEffects>();if(!fx)throw new Exception("Open Gameplay.");
  var config=new SerializedObject(fx);var positions=config.FindProperty("burstPositions");var systems=config.FindProperty("bursts");
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/BoldVFXPackDemo/Content/URP/Prefabs/FX_Click_Yellow.prefab");
  for(int i=0;i<positions.arraySize;i++){
   var slot=(RectTransform)positions.GetArrayElementAtIndex(i).objectReferenceValue;
   for(int j=slot.childCount-1;j>=0;j--)UnityEngine.Object.DestroyImmediate(slot.GetChild(j).gameObject);
   var effect=(GameObject)PrefabUtility.InstantiatePrefab(prefab,slot);effect.transform.localPosition=Vector3.zero;effect.transform.localRotation=Quaternion.identity;effect.transform.localScale=Vector3.one;
   systems.GetArrayElementAtIndex(i).objectReferenceValue=effect.GetComponent<ParticleSystem>();
   foreach(var ps in effect.GetComponentsInChildren<ParticleSystem>(true)){
    ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    var main=ps.main;main.loop=false;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;main.maxParticles=32;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;main.simulationSpace=ParticleSystemSimulationSpace.Local;
    ps.GetComponent<ParticleSystemRenderer>().enabled=false;
    var sheet=ps.textureSheetAnimation;
    var emission=ps.emission;emission.rateOverTime=0;emission.rateOverDistance=0;
    if(!sheet.enabled||sheet.mode!=ParticleSystemAnimationMode.Sprites||sheet.spriteCount==0){emission.enabled=false;continue;}
    emission.enabled=true;emission.SetBursts(new[]{new ParticleSystem.Burst(0,1)});
    var go=new GameObject(ps.name+"Canvas",typeof(RectTransform),typeof(CanvasRenderer),typeof(CanvasParticleGraphic));var rect=go.GetComponent<RectTransform>();rect.SetParent(slot,false);rect.sizeDelta=new Vector2(220,220);
    var graphic=go.GetComponent<CanvasParticleGraphic>();graphic.raycastTarget=false;
    var so=new SerializedObject(graphic);so.FindProperty("source").objectReferenceValue=ps;so.FindProperty("particleSprite").objectReferenceValue=sheet.GetSprite(0);so.FindProperty("pixelsPerUnit").floatValue=35;so.ApplyModifiedPropertiesWithoutUndo();
   }
  }
  config.ApplyModifiedPropertiesWithoutUndo();
  EditorSceneManager.MarkSceneDirty(fx.gameObject.scene);EditorSceneManager.SaveScene(fx.gameObject.scene);Selection.activeGameObject=fx.gameObject;
  return "Replaced four star bursts with one-shot FX_Click_Yellow; 8 saved Canvas renderers.";
 }
}
