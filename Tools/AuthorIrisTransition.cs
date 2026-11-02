using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;

public static class AuthorIrisTransition
{
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before authoring.");
  var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string original=current.path;
  if(current.isDirty)EditorSceneManager.SaveScene(current);
  const string folder="Assets/UI/Transitions";
  if(!AssetDatabase.IsValidFolder("Assets/UI"))AssetDatabase.CreateFolder("Assets","UI");
  if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/UI","Transitions");
  var shader=Shader.Find("RhythmRehab/UI/IrisTransition");if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Iris shader did not compile.");
  string materialPath=folder+"/IrisTransition.mat";
  var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
  if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
  material.SetFloat("_Radius",2);material.SetFloat("_Aspect",16f/9);EditorUtility.SetDirty(material);
  string prefabPath=folder+"/SceneTransition.prefab";
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
  if(!prefab)
  {
   var root=new GameObject("SceneTransition",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(IrisSceneTransition));
   root.layer=5;var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
   var overlay=new GameObject("IrisOverlay",typeof(RectTransform),typeof(RawImage));overlay.layer=5;overlay.transform.SetParent(root.transform,false);
   var rect=(RectTransform)overlay.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
   var image=overlay.GetComponent<RawImage>();image.texture=Texture2D.whiteTexture;image.color=Color.white;image.material=material;image.raycastTarget=false;
   var so=new SerializedObject(root.GetComponent<IrisSceneTransition>());so.FindProperty("irisImage").objectReferenceValue=image;so.ApplyModifiedProperties();
   prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);UnityEngine.Object.DestroyImmediate(root);
  }
  AssetDatabase.SaveAssets();int count=0;
  foreach(var path in Directory.GetFiles("Assets/Scenes","*.unity"))
  {
   var scene=EditorSceneManager.OpenScene(path);
   var transitions=UnityEngine.Object.FindObjectsByType<IrisSceneTransition>();
   if(transitions.Length>1)throw new Exception("Multiple saved transitions in "+scene.name);
   if(transitions.Length==0)PrefabUtility.InstantiatePrefab(prefab,scene);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);count++;
  }
  EditorSceneManager.OpenScene(original);
  return "Iris shader compiled; shared SceneTransition prefab saved in "+count+" scenes. Close .22s / open .28s.";
 }
}
