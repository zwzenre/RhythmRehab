using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// Run once in the Editor. Decorations are serialized objects, not runtime-generated UI.
public static class DecorateScenes
{
 const string Assets="Assets/Assets/Space_Exploration_GUI_Kit/Other_Images/Large/";
 static RectTransform Group(Transform parent,string name)
 {
  var child=parent.Find(name);
  if(child)return (RectTransform)child;
  var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Decorate game scene");
  go.layer=LayerMask.NameToLayer("UI");go.transform.SetParent(parent,false);
  var rt=(RectTransform)go.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;
  rt.offsetMin=rt.offsetMax=Vector2.zero;return rt;
 }
 static void Art(Transform parent,string name,string file,Vector2 anchor,Vector2 position,float width,Color color,bool flip=false)
 {
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Assets+file+"-large.png");
  if(!texture)throw new Exception("Missing decoration: "+file);
  var existing=parent.Find(name);
  var go=existing?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(RawImage));
  if(!existing){Undo.RegisterCreatedObjectUndo(go,"Place GUI decoration");go.transform.SetParent(parent,false);}
  go.layer=LayerMask.NameToLayer("UI");
  var rt=(RectTransform)go.transform;Undo.RecordObject(rt,"Position GUI decoration");
  rt.anchorMin=rt.anchorMax=anchor;rt.pivot=Vector2.one*.5f;rt.anchoredPosition=position;
  rt.sizeDelta=new Vector2(width,width*texture.height/texture.width);rt.localScale=Vector3.one;rt.localRotation=Quaternion.identity;
  var raw=go.GetComponent<RawImage>();Undo.RecordObject(raw,"Set decorative texture");
  raw.texture=texture;raw.color=color;raw.raycastTarget=false;raw.uvRect=flip?new Rect(1,0,-1,1):new Rect(0,0,1,1);
  EditorUtility.SetDirty(raw);
 }
 public static string Main()
 {
  var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(current.isDirty)throw new Exception("The open scene has unsaved changes. Preserve them before decorating.");
  string original=current.path;int count=0;
  foreach(var path in Directory.GetFiles("Assets/Scenes","*.unity").OrderBy(p=>p))
  {
   var scene=EditorSceneManager.OpenScene(path);bool menu=scene.name=="MainMenu";
   var canvas=UnityEngine.Object.FindObjectsByType<Canvas>().Single(c=>c.isRootCanvas);
   var bg=canvas.transform.Find("Background");if(!bg)throw new Exception("Missing background in "+scene.name);
   var shade=bg.Find(menu?"MidnightTint":"DimOverlay");if(!shade)throw new Exception("Missing dim overlay in "+scene.name);
   var clouds=Group(bg,"DecorativeClouds");clouds.SetSiblingIndex(shade.GetSiblingIndex());
   var imageColor=bg.Find("SpaceBackdrop").GetComponent<RawImage>().color;
   imageColor.a=menu?.48f:.42f;
   // Anchor to screen edges, keeping the centre and note lanes unobstructed.
   Art(clouds,"CloudLeft","cloud-1",new Vector2(0,0),new Vector2(95,25),510,imageColor);
   Art(clouds,"CloudRight","cloud-2",new Vector2(1,0),new Vector2(-70,45),400,imageColor,true);
   var stars=Group(bg,"DecorativeStarlight");stars.SetAsLastSibling();
   bool blue=scene.name=="SongSelection"||scene.name=="ModeSelection"||scene.name=="Settings";
   Art(stars,"HeaderShootingStar",blue?"shooting-star-blue":"shooting-star-pink-short",new Vector2(.5f,1),new Vector2(menu?580:-560,-83),menu?150:175,new Color(.8f,.83f,1,menu?.46f:.32f));
   Art(stars,"SparkleLeft","sparkle",new Vector2(0,.5f),new Vector2(28,130),20,new Color(.65f,.58f,1,.32f));
   Art(stars,"SparkleRight","sparkle",new Vector2(1,.5f),new Vector2(-26,-200),14,new Color(.45f,.8f,1,.27f));
   Art(stars,"SparkleBottom","sparkle",new Vector2(.5f,0),new Vector2(10,20),11,new Color(.9f,.64f,1,.23f));
   if(menu)
   {
    var character=canvas.transform.Find("GuyLiftingLeg") as RectTransform;
    if(character)
    {
     // Match the actual image's lower edge in canvas coordinates.
     var corners=new Vector3[4];character.GetWorldCorners(corners);
     Vector3 left=canvas.transform.InverseTransformPoint(corners[0]),right=canvas.transform.InverseTransformPoint(corners[3]);
     var ground=Group(canvas.transform,"CharacterGrounding");ground.SetSiblingIndex(character.GetSiblingIndex());
     Art(ground,"CharacterShadow","object-shadow",new Vector2(.5f,.5f),new Vector2((left.x+right.x)*.5f,left.y+12),Mathf.Min(380,character.rect.width*.7f),new Color(.65f,.45f,1,.46f));
    }
   }
   if(bg.GetComponentsInChildren<RawImage>().Any(g=>g.raycastTarget))throw new Exception("A background graphic blocks input in "+scene.name);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);count++;
  }
  EditorSceneManager.OpenScene(original);return $"Decorated {count} scenes with saved cloud, shooting-star and sparkle objects; added character shadow to Main Menu. All graphics ignore raycasts.";
 }
}
