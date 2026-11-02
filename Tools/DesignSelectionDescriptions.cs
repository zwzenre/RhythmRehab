using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class DesignSelectionDescriptions
{
 static readonly Color White=new Color(.94f,.96f,1),Muted=new Color(.58f,.65f,.8f);
 const string Icons="Assets/Assets/Space_Exploration_GUI_Kit/Picto_Icons/White/";
 static GameObject Node(Transform parent,string name,Vector2 position,Vector2 size)
 {
  var found=parent.Find(name);var go=found?found.gameObject:new GameObject(name,typeof(RectTransform));
  if(!found){Undo.RegisterCreatedObjectUndo(go,"Design selection descriptions");go.transform.SetParent(parent,false);}
  go.layer=5;var rect=(RectTransform)go.transform;Undo.RecordObject(rect,"Position description element");
  rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;return go;
 }
 static Color Alpha(Color c,float alpha){c.a=alpha;return c;}
 static GameObject Panel(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
 {
  var go=Node(parent,name,pos,size);var image=go.GetComponent<Image>()??Undo.AddComponent<Image>(go);
  image.color=color;image.raycastTarget=false;EditorUtility.SetDirty(image);return go;
 }
 static void Label(Transform parent,string name,Vector2 pos,Vector2 size,string value,float fontSize,Color color,TMP_Text style,bool center=false)
 {
  var go=Node(parent,name,pos,size);var label=go.GetComponent<TextMeshProUGUI>()??Undo.AddComponent<TextMeshProUGUI>(go);
  label.font=style.font;label.fontSharedMaterial=style.fontSharedMaterial;label.text=value;label.fontSize=fontSize;
  label.enableAutoSizing=false;label.color=color;label.alignment=center?TextAlignmentOptions.Center:TextAlignmentOptions.MidlineLeft;
  label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Truncate;label.raycastTarget=false;EditorUtility.SetDirty(label);
 }
 static void Icon(Transform parent,string name,string file,Vector2 pos,float size,Color color)
 {
  var sprite=AssetDatabase.LoadAllAssetsAtPath(Icons+file+"-128.png").OfType<Sprite>().OrderByDescending(s=>s.rect.width*s.rect.height).FirstOrDefault();
  if(!sprite)throw new Exception("Missing GUI-kit icon: "+file);
  var go=Node(parent,name,pos,new Vector2(size,size));var image=go.GetComponent<Image>()??Undo.AddComponent<Image>(go);
  image.sprite=sprite;image.color=color;image.preserveAspect=true;image.raycastTarget=false;EditorUtility.SetDirty(image);
 }
 static void Feature(Transform parent,string name,float y,float width,float height,string icon,string heading,string detail,Color accent,TMP_Text style,bool large)
 {
  var row=Panel(parent,name,new Vector2(0,y),new Vector2(width,height),Alpha(accent,.07f));
  Panel(row.transform,"Accent",new Vector2(-width/2+2,0),new Vector2(3,height-12),Alpha(accent,.85f));
  float iconX=-width/2+(large?36:32);
  var tile=Panel(row.transform,"IconTile",new Vector2(iconX,0),new Vector2(large?48:34,large?48:34),Alpha(accent,.12f));
  Icon(tile.transform,"Icon",icon,Vector2.zero,large?30:24,accent);
  float left=-width/2+(large?76:60),right=width/2-12;
  float x=(left+right)*.5f,w=right-left;
  Label(row.transform,"Heading",new Vector2(x,large?15:12),new Vector2(w,large?23:18),heading,large?18:15,accent,style);
  Label(row.transform,"Detail",new Vector2(x,large?-12:-8),new Vector2(w,large?35:29),detail,large?25:22,White,style);
 }
 static Transform Card(string path)
 {
  var go=GameObject.Find(path);if(!go||!go.transform.Find("Description"))throw new Exception("Missing existing description: "+path);
  return go.transform;
 }
 static TMP_Text HideDescription(Transform card)
 {
  var text=card.Find("Description").GetComponent<TMP_Text>();Undo.RecordObject(text.gameObject,"Replace plain description with visual rows");text.gameObject.SetActive(false);return text;
 }
 static void Mode(string name,Color accent,bool free)
 {
  var card=Card("/Canvas/"+name);var style=HideDescription(card);
  var root=Node(card,"DescriptionDesign",Vector2.zero,new Vector2(700,500)).transform;
  Icon(card,"ModeIcon",free?"music":"heart",new Vector2(-290,155),48,accent);
  Feature(root,"HeightFeature",30,650,70,free?"shuffle-arrow":"lock",free?"CONTINUOUS HEIGHTS":"ONE TARGET",free?"Notes between LOW, MID and HIGH":"Every note stays at the chosen height",accent,style,true);
  Feature(root,"ChoiceFeature",-52,650,70,free?"star":"up-arrow",free?"CHOOSE YOUR CHALLENGE":"CHOOSE A HEIGHT",free?"EASY  /  MEDIUM  /  HARD":"LOW  /  MEDIUM  /  HIGH",accent,style,true);
  Feature(root,"RhythmFeature",-134,650,70,free?"bolt":"music",free?"CHANGING PATTERNS":"STEADY PRACTICE",free?"Follow notes at different heights":"Hold your height and hit on the beat",accent,style,true);
  var foot=card.Find("Footnote");Undo.RecordObject(foot.gameObject,"Replace footer with mode badge");foot.gameObject.SetActive(false);
  var badge=Panel(root,"ModeBadge",new Vector2(0,-215),new Vector2(650,38),Alpha(accent,.13f));
  Label(badge.transform,"Label",Vector2.zero,new Vector2(620,36),free?"MIXED HEIGHT CHALLENGE":"FIXED HEIGHT PRACTICE",21,accent,style,true);
 }
 static void Exercise(int index,Color accent)
 {
  var card=Card("/Canvas/Choice"+index);var style=HideDescription(card);var level=card.Find("Level");
  if(!level)throw new Exception("Exercise level label missing.");level.gameObject.SetActive(false);
  var meter=Node(card,"HeightIndicator",new Vector2(0,65),new Vector2(430,70));
  Panel(meter.transform,"Baseline",new Vector2(-80,-12),new Vector2(144,2),new Color(.16f,.19f,.3f));
  for(int j=0;j<3;j++)
  {
   float h=14+j*12;
   Panel(meter.transform,"Bar"+j,new Vector2(-130+j*50,-12+h*.5f),new Vector2(28,h),j==index?accent:new Color(.14f,.18f,.28f));
   Label(meter.transform,"Level"+j,new Vector2(-130+j*50,-28),new Vector2(35,22),new[]{"L","M","H"}[j],15,j==index?accent:Muted,style,true);
  }
  var badge=Panel(meter.transform,"LevelBadge",new Vector2(110,0),new Vector2(170,44),Alpha(accent,.13f));
  Label(badge.transform,"Text",Vector2.zero,new Vector2(165,44),"LEVEL "+(index+1),25,accent,style,true);
  var root=Node(card,"DescriptionDesign",Vector2.zero,new Vector2(450,500)).transform;
  Feature(root,"TargetFeature",-40,430,48,"up-arrow","TARGET HEIGHT",new[]{"LOW only","MEDIUM only","HIGH only"}[index],accent,style,false);
  Feature(root,"PatternFeature",-98,430,48,"music","NOTE PATTERN","32 evenly spaced notes",accent,style,false);
  Feature(root,"HoldFeature",-156,430,48,"lock","MOVEMENT","Hold your chosen level",accent,style,false);
 }
 static void Difficulty(int index,Color accent)
 {
  var card=Card("/Canvas/Choice"+index);var style=HideDescription(card);
  var root=Node(card,"DescriptionDesign",Vector2.zero,new Vector2(450,500)).transform;
  Feature(root,"CountFeature",-40,430,48,"music","NOTE COUNT",new[]{"16 notes","24 notes","32 notes"}[index],accent,style,false);
  Feature(root,"RangeFeature",-98,430,48,"shuffle-arrow","HEIGHT RANGE",new[]{"LOW to MID","LOW to HIGH","Larger height changes"}[index],accent,style,false);
  Feature(root,"TimingFeature",-156,430,48,"bolt","TIMING WINDOW",new[]{"Wider window","Standard window","Tighter window"}[index],accent,style,false);
 }
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before authoring.");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string original=scene.path;
  if(scene.isDirty)EditorSceneManager.SaveScene(scene);
  Color green=new Color(.2f,.9f,.45f),cyan=new Color(.12f,.85f,1),red=new Color(1,.25f,.4f),purple=new Color(.63f,.3f,1),yellow=new Color(1,.7f,.1f);
  EditorSceneManager.OpenScene("Assets/Scenes/ModeSelection.unity");Mode("TherapyCard",purple,false);Mode("FreeCard",cyan,true);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  EditorSceneManager.OpenScene("Assets/Scenes/ExerciseSelection.unity");Color[] heights={green,yellow,red};for(int i=0;i<3;i++)Exercise(i,heights[i]);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  EditorSceneManager.OpenScene("Assets/Scenes/DifficultySelection.unity");Color[] difficulties={green,cyan,red};for(int i=0;i<3;i++)Difficulty(i,difficulties[i]);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  EditorSceneManager.OpenScene(original);return "Three selection scenes redesigned with GUI-kit icons, feature tiles, mode badges and exercise height indicators. Buttons and difficulty stars preserved.";
 }
}
