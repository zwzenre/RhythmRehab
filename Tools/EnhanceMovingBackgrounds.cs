using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class EnhanceMovingBackgrounds
{
 const string Kit="Assets/Assets/Space_Exploration_GUI_Kit/";
 static void Motion(GameObject go,Vector2 amplitude,float period,float phase,float rotation=0,float scale=0,bool texture=false)
 {
  var m=go.GetComponent<GentleBackgroundMotion>();if(!m)m=go.AddComponent<GentleBackgroundMotion>();var so=new SerializedObject(m);
  so.FindProperty("motionType").enumValueIndex=texture?0:1;so.FindProperty("amplitude").vector2Value=amplitude;so.FindProperty("period").floatValue=period;so.FindProperty("phase").floatValue=phase;so.FindProperty("textureWindow").floatValue=.92f;so.FindProperty("rotationSway").floatValue=rotation;so.FindProperty("scaleSway").floatValue=scale;so.ApplyModifiedPropertiesWithoutUndo();
 }
 static RectTransform Group(Transform parent,string name){var found=parent.Find(name);if(found)return (RectTransform)found;var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
 static void Art(Transform parent,string name,string path,Vector2 anchor,Vector2 position,float width,Color color,Vector2 amplitude,float period,float phase,float rotate=0,float scale=0)
 {
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Kit+path);if(!texture)throw new Exception("Missing decoration "+path);
  var found=parent.Find(name);var go=found?found.gameObject:new GameObject(name,typeof(RectTransform),typeof(RawImage));if(!found)go.transform.SetParent(parent,false);go.layer=5;var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=new Vector2(width,width*texture.height/texture.width);r.localScale=Vector3.one;r.localRotation=Quaternion.identity;
  var image=go.GetComponent<RawImage>();image.texture=texture;image.color=color;image.raycastTarget=false;EditorUtility.SetDirty(image);Motion(go,amplitude,period,phase,rotate,scale);
 }
 public static string Main(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");var current=SceneManager.GetActiveScene();if(current.isDirty)throw new Exception("Save current scene first.");string original=current.path;var report=new List<string>();
  foreach(var entry in EditorBuildSettings.scenes){if(!entry.enabled||System.IO.Path.GetFileNameWithoutExtension(entry.path)=="Gameplay")continue;
   var scene=EditorSceneManager.OpenScene(entry.path);var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RawImage>(true)).ToArray();var space=all.First(i=>i.name=="SpaceBackdrop");var bg=space.transform.parent;var shade=bg.Find(scene.name=="MainMenu"?"MidnightTint":"DimOverlay");if(!shade)throw new Exception("Missing dim overlay in "+scene.name);
   Motion(space.gameObject,new Vector2(.026f,.018f),23,0,texture:true);
   foreach(var image in all){if(!image.transform.parent)continue;
    if(image.transform.parent.name=="DecorativeClouds")Motion(image.gameObject,new Vector2(48,22),17+image.transform.GetSiblingIndex()*3,image.transform.GetSiblingIndex()*1.7f+.6f);
    if(image.transform.parent.name=="DecorativeStarlight"){int i=image.transform.GetSiblingIndex();Motion(image.gameObject,image.name=="HeaderShootingStar"?new Vector2(38,18):new Vector2(22,13),12+i*2,i*1.7f+.6f);}
   }
   // Place the large artwork under the same dim overlay as the original background.
   var deep=Group(bg,"MovingSpaceDecorations");deep.SetAsLastSibling();deep.SetSiblingIndex(shade.GetSiblingIndex());
   Art(deep,"RingPlanetEdge","Icons/exoplanet-5-512.png",Vector2.one,new Vector2(-70,-270),370,new Color(.72f,.64f,1,.8f),new Vector2(42,25),19,1.1f,4,.025f);
   Art(deep,"BluePlanetEdge","Icons/exoplanet-3-512.png",Vector2.zero,new Vector2(20,155),270,new Color(.42f,.64f,1,.72f),new Vector2(38,22),21,2.8f,3,.02f);
   Art(deep,"DistantPlanet","Icons/saturn-512.png",new Vector2(0,1),new Vector2(460,-75),112,new Color(.5f,.58f,1,.6f),new Vector2(30,16),22,.8f,5,.02f);
   Art(deep,"FloatingSatellite","Icons/satellite-512.png",new Vector2(0,1),new Vector2(85,-205),115,new Color(.55f,.7f,1,.62f),new Vector2(32,19),17,3.3f,6);
   Art(deep,"UpperCloudHaze","Other_Images/Large/cloud-2-large.png",new Vector2(0,1),new Vector2(30,-155),520,new Color(.62f,.5f,.9f,.65f),new Vector2(58,24),18,2.4f);
   Art(deep,"RightCloudHaze","Other_Images/Large/cloud-1-large.png",new Vector2(1,.5f),new Vector2(-25,-65),440,new Color(.5f,.55f,.9f,.62f),new Vector2(52,24),20,4.2f);
   var dust=Group(bg,"MovingAmbientStars");dust.SetAsLastSibling();
   Vector2[] anchors={new Vector2(.04f,.7f),new Vector2(.14f,.86f),new Vector2(.28f,.96f),new Vector2(.63f,.94f),new Vector2(.88f,.88f),new Vector2(.97f,.6f),new Vector2(.94f,.23f),new Vector2(.77f,.12f),new Vector2(.6f,.06f),new Vector2(.34f,.08f),new Vector2(.12f,.2f),new Vector2(.04f,.46f),new Vector2(.8f,.47f),new Vector2(.98f,.92f)};
   for(int i=0;i<anchors.Length;i++){
    bool sparkle=i%3==0;Art(dust,"AmbientStar"+i,sparkle?"Other_Images/Large/sparkle-large.png":"Picto_Icons/White/full-moon-128.png",anchors[i],Vector2.zero,sparkle?12+i%4*2:3+i%3,new Color(.55f+i%2*.2f,.62f,.95f,.2f+i%3*.035f),new Vector2(22+i%3*5,13+i%2*5),11+i%5*2,i*.8f,sparkle?8:0);
   }
   Art(dust,"DriftingShootingStar","Other_Images/Large/shooting-star-blue-large.png",new Vector2(1,0),new Vector2(-200,300),210,new Color(.65f,.65f,1,.32f),new Vector2(56,24),15,1.8f,3);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(scene.name+": stronger drift + "+deep.childCount+" deep decorations + "+dust.childCount+" floating stars/trail");
  }
  if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);return string.Join("\n",report)+"\nAll decorations ignore pointer input. Gameplay excluded.";
 }
}
