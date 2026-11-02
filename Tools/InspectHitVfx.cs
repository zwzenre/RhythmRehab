using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class InspectHitVfx
{
 public static string Main(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 var text="Scene="+scene.path+" dirty="+scene.isDirty+" playing="+EditorApplication.isPlaying+"\n";
 foreach(var name in new[]{"FX_Click_Yellow","FX_Sparkles_Yellow","FX_Stars_Yellow"}){
 var go=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/BoldVFXPackDemo/Content/URP/Prefabs/"+name+".prefab");
 text+=name+"\n";
 foreach(var p in go.GetComponentsInChildren<ParticleSystem>(true)){
 var m=p.main;var e=p.emission;var r=p.GetComponent<ParticleSystemRenderer>();var t=p.textureSheetAnimation;
 text+=$"{p.name} pos={p.transform.localPosition} scale={p.transform.localScale} loop={m.loop} duration={m.duration} lifetime={m.startLifetime.constantMax} size={m.startSize.constantMax} speed={m.startSpeed.constantMax} max={m.maxParticles} simulation={m.simulationSpace} bursts={e.burstCount} material={AssetDatabase.GetAssetPath(r.sharedMaterial)} texture={AssetDatabase.GetAssetPath(r.sharedMaterial ? r.sharedMaterial.mainTexture : null)} sheet={t.enabled} mode={t.mode} sprites={t.spriteCount}\n";
 }
 }
 var canvas=UnityEngine.Object.FindAnyObjectByType<Canvas>();if(canvas)text+="Canvas="+canvas.renderMode+" size="+canvas.GetComponent<RectTransform>().rect+"\n";
 return text;
 }
}
