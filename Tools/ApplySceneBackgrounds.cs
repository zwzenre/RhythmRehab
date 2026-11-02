using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// Editor-only authoring command. All results are saved, Inspector-editable scene objects.
public static class ApplySceneBackgrounds
{
 const string TexturePath="Assets/Assets/Space_Exploration_GUI_Kit/Background_Images/large/background-2-large.png";
 const string MenuPath="Assets/Scenes/MainMenu.unity";
 static RectTransform Child(Transform parent,string name)
 {
  var existing=parent.Find(name);
  if(existing)return (RectTransform)existing;
  var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Add scene background");
  go.transform.SetParent(parent,false);go.layer=LayerMask.NameToLayer("UI");return (RectTransform)go.transform;
 }
 static void Fill(RectTransform rect)
 {
  rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=Vector2.one*.5f;
  rect.offsetMin=rect.offsetMax=Vector2.zero;rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
 }
 public static string Main()
 {
  var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(active.isDirty)throw new Exception("The open scene has unsaved changes; preserve them before applying backgrounds.");
  string original=active.path;byte[] menuBytes=File.ReadAllBytes(MenuPath);
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
  if(!texture)throw new Exception("Background texture missing: "+TexturePath);
  EditorSceneManager.OpenScene(MenuPath);
  var source=GameObject.Find("/MenuCanvas/Background/SpaceBackdrop").GetComponent<RawImage>();
  var tint=GameObject.Find("/MenuCanvas/Background/MidnightTint").GetComponent<Image>();
  const float brightness=.8f;
  Color imageColor=new Color(source.color.r*brightness,source.color.g*brightness,source.color.b*brightness,1);
  Color overlayColor=new Color(tint.color.r*brightness,tint.color.g*brightness,tint.color.b*brightness,tint.color.a);
  int changed=0;
  foreach(string path in Directory.GetFiles("Assets/Scenes","*.unity").OrderBy(p=>p))
  {
   if(path.Replace('\\','/')==MenuPath)continue;
   var scene=EditorSceneManager.OpenScene(path);
   var canvas=UnityEngine.Object.FindObjectsByType<Canvas>().Single(c=>c.isRootCanvas);
   var background=canvas.transform.Find("Background");
   if(!background||!background.GetComponent<Image>())throw new Exception("Missing existing UI background in "+scene.name);
   Undo.RecordObject(background,"Configure scene background");
   background.SetAsFirstSibling();
   var art=Child(background,"SpaceBackdrop");Fill(art);art.SetAsFirstSibling();
   var raw=art.GetComponent<RawImage>()??Undo.AddComponent<RawImage>(art.gameObject);
   Undo.RecordObject(raw,"Set GUI kit backdrop");raw.texture=texture;raw.color=imageColor;raw.raycastTarget=false;
   var aspect=art.GetComponent<AspectRatioFitter>()??Undo.AddComponent<AspectRatioFitter>(art.gameObject);
   aspect.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;aspect.aspectRatio=(float)texture.width/texture.height;
   var dim=Child(background,"DimOverlay");Fill(dim);dim.SetAsLastSibling();
   var shade=dim.GetComponent<Image>()??Undo.AddComponent<Image>(dim.gameObject);
   Undo.RecordObject(shade,"Dim secondary scene backdrop");shade.color=overlayColor;shade.raycastTarget=false;
   EditorUtility.SetDirty(raw);EditorUtility.SetDirty(aspect);EditorUtility.SetDirty(shade);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);changed++;
  }
  if(!menuBytes.SequenceEqual(File.ReadAllBytes(MenuPath)))throw new Exception("Main Menu unexpectedly changed.");
  EditorSceneManager.OpenScene(original);
  return $"Updated {changed} secondary scenes with {TexturePath}. Image and tint RGB are 80% of Main Menu; Main Menu bytes unchanged.";
 }
}
