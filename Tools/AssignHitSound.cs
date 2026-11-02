using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class AssignHitSound
{
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();if(!game)throw new Exception("Open Gameplay first.");
  var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Assets/mixkit-short-slap-or-clap-2167.wav");if(!clip)throw new Exception("Hit audio has not imported.");
  var config=GameplayEditorAccess.Owner(game,"hitSource");var source=(AudioSource)config.FindProperty("hitSource").objectReferenceValue;
  source.clip=clip;source.playOnAwake=false;source.loop=false;source.spatialBlend=0;
  EditorUtility.SetDirty(source);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);
  return $"Assigned {clip.name} ({clip.length:0.00}s) to {source.name}; 2D one-shot, play-on-awake off. SFX volume preference retained.";
 }
}
