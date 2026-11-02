using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;

public static class BuildWellbeingAssessment
{
 const string Kit="Assets/Assets/Space_Exploration_GUI_Kit/";
 static readonly Color White=new Color(.94f,.96f,1),Muted=new Color(.62f,.68f,.82f),Purple=new Color(.72f,.36f,1),Cyan=new Color(.15f,.8f,1),Dark=new Color(.025f,.03f,.075f,.98f);
 static TMP_FontAsset font;
 static void Rect(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
 static GameObject Node(Transform parent,string name,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.layer=5;if(parent)go.transform.SetParent(parent,false);Rect((RectTransform)go.transform,x,y,w,h);return go;}
 static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
 static Image Panel(Transform p,string n,float x,float y,float w,float h,Color color,Color? outline=null){var image=Node(p,n,x,y,w,h).AddComponent<Image>();image.color=color;image.raycastTarget=false;if(outline.HasValue){var edge=image.gameObject.AddComponent<Outline>();edge.effectColor=outline.Value;edge.effectDistance=new Vector2(2,-2);}return image;}
 static TMP_Text Text(Transform p,string n,float x,float y,float w,float h,string content,float size,Color color,bool center=false){var t=Node(p,n,x,y,w,h).AddComponent<TextMeshProUGUI>();t.font=font;t.fontSharedMaterial=font.material;t.text=content;t.fontSize=size;t.color=color;t.alignment=center?TextAlignmentOptions.Center:TextAlignmentOptions.MidlineLeft;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Ellipsis;t.raycastTarget=false;return t;}
 static Sprite Sprite(string name)=>AssetDatabase.LoadAllAssetsAtPath(Kit+"Picto_Icons/White/"+name+"-128.png").OfType<Sprite>().OrderByDescending(s=>s.rect.width*s.rect.height).First();
 static Image Icon(Transform p,string n,string file,float x,float y,float size,Color color){var i=Panel(p,n,x,y,size,size,color);i.sprite=Sprite(file);i.preserveAspect=true;return i;}
 static Button Button(Transform p,string n,float x,float y,float w,float h,string label,UnityAction action,bool blue=false){string color=blue?"Blue":"Purple";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Kit+"Prefabs/Buttons_Simple/Large/"+color+"/Long_"+color+"_Large_Button.prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=n;go.transform.SetParent(p,false);Rect((RectTransform)go.transform,x,y,w,h);var b=go.GetComponent<Button>();b.targetGraphic=go.GetComponent<Image>();b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);var t=go.GetComponentInChildren<TMP_Text>();t.text=label;t.fontSize=27;t.color=White;t.raycastTarget=false;return b;}
 static void Bind(MonoBehaviour target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static Slider Slider(Transform p,string name,Color accent){var go=Node(p,name,0,5,690,50);var hit=go.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;var slider=go.AddComponent<UnityEngine.UI.Slider>();var track=Panel(go.transform,"Track",0,0,650,9,new Color(.12f,.13f,.25f));var fill=Panel(track.transform,"Fill",0,0,650,9,accent);Stretch((RectTransform)fill.transform);var area=Node(go.transform,"HandleArea",0,0,650,42);var handle=Panel(area.transform,"Handle",0,0,30,-12,accent);handle.sprite=Sprite("full-moon");handle.preserveAspect=true;handle.raycastTarget=true;slider.fillRect=(RectTransform)fill.transform;slider.handleRect=(RectTransform)handle.transform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=10;slider.wholeNumbers=true;slider.SetValueWithoutNotify(0);return slider;}
 static void RatingCard(Transform canvas,string name,float x,Color accent,string icon,string title,string question,string first,string last,WellbeingAssessmentController controller,string prefix){
  var card=Panel(canvas,name,x,-25,800,430,Dark,new Color(accent.r,accent.g,accent.b,.6f)).transform;
  Icon(card,"Icon",icon,-320,125,58,accent);Text(card,"Title",-70,143,410,55,title,34,accent);Text(card,"Question",-45,94,460,65,question,23,Muted);
  var badge=Panel(card,"ValueBadge",285,130,155,62,new Color(accent.r*.2f,accent.g*.2f,accent.b*.2f,1),accent).transform;Bind(controller,prefix+"Value",Text(badge,"Value",0,0,145,55,"0 / 10",30,accent,true));
  var slider=Slider(card,"Slider",accent);Bind(controller,prefix+"Slider",slider);UnityEventTools.AddPersistentListener(slider.onValueChanged,controller.Refresh);
  for(int i=0;i<=10;i++)Text(card,"Tick"+i,-325+i*65,-37,45,30,i.ToString(),19,Muted,true);
  Text(card,"Minimum",-215,-80,300,32,first,21,Muted);var max=Text(card,"Maximum",215,-80,300,32,last,21,Muted);max.alignment=TextAlignmentOptions.Right;
  Panel(card,"MessagePanel",0,-154,720,100,new Color(accent.r*.09f,accent.g*.09f,accent.b*.09f,1));Bind(controller,prefix+"Message",Text(card,"Message",0,-154,680,90,"",24,accent,true));
 }
 public static string Main(){
  const string path="Assets/Scenes/WellbeingAssessment.unity";
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
  if(File.Exists(path))throw new Exception("Assessment scene already exists.");
  if(SceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene first.");
  font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Kit+"Fonts/Asap-Bold SDF.asset");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-10);var cam=camera.GetComponent<Camera>();cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.01f,.01f,.03f);
  var go=Node(null,"Canvas",0,0,1920,1080);var canvas=go.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;go.AddComponent<GraphicRaycaster>();var scaler=go.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;var c=go.transform;
  var backdrop=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Settings/SettingsBackdrop.prefab"),scene);backdrop.transform.SetParent(c,false);Stretch((RectTransform)backdrop.transform);
  new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));var controller=new GameObject("Controllers").AddComponent<WellbeingAssessmentController>();
  Button(c,"BackButton",-820,450,180,70,"BACK",controller.Back,true);Button(c,"HomeButton",820,450,180,70,"HOME",controller.Back,true);
  Icon(c,"HeaderMedallion","full-moon",0,428,96,new Color(.13f,.06f,.25f));Icon(c,"HeaderHeart","heart",0,428,52,Purple);
  Text(c,"Title",0,329,1650,75,"PRE-SESSION WELLBEING ASSESSMENT",46,White,true);
  Text(c,"Subtitle",0,253,1600,80,"How are you feeling today?\nRate your current pain and stress before you play.",27,Muted,true);
  RatingCard(c,"PainCard",-440,Purple,"heart","PAIN LEVEL","How much pain are you feeling right now?","No pain","Severe pain",controller,"pain");
  RatingCard(c,"StressCard",440,Cyan,"leaf","STRESS LEVEL","How stressed are you feeling right now?","Low stress","High stress",controller,"stress");
  var note=Panel(c,"FeedbackNotice",0,-326,1680,112,new Color(.025f,.04f,.09f,.98f),new Color(.1f,.35f,.65f)).transform;Icon(note,"InfoIcon","info",-770,0,45,Cyan);Bind(controller,"feedbackNotice",Text(note,"Text",45,0,1480,95,"",24,Muted,true));
  Text(c,"PrivacyNote",-360,-463,970,80,"OPTIONAL CHECK-IN\nYour ratings are stored locally with the session you complete.",22,Muted);
  Button(c,"SkipButton",420,-460,230,76,"SKIP",controller.Skip,true);Button(c,"ContinueButton",735,-460,350,76,"CONTINUE",controller.Continue);
  var overlay=Panel(c,"MessageOverlay",0,0,1920,1080,new Color(0,0,.02f,.82f));Stretch((RectTransform)overlay.transform);overlay.raycastTarget=true;
  var dialog=Panel(overlay.transform,"Dialog",0,0,1040,690,new Color(.035f,.025f,.09f,1),Purple).transform;
  Icon(dialog,"IconBackplate","full-moon",0,245,104,new Color(.12f,.05f,.22f));Bind(controller,"messageIcon",Icon(dialog,"MessageIcon","caution",0,245,66,Purple));
  Bind(controller,"messageTitle",Text(dialog,"Title",0,153,930,90,"",33,White,true));Bind(controller,"messageBody",Text(dialog,"Body",0,-20,900,240,"",27,Muted,true));
  Button(dialog,"ReviewButton",-330,-250,280,72,"REVIEW RATINGS",controller.Review,true);Button(dialog,"MenuButton",0,-250,270,72,"BACK TO MENU",controller.Back,true);Button(dialog,"AcknowledgeButton",330,-250,280,72,"GOT IT",controller.Acknowledge);
  Button(dialog,"CloseButton",465,285,60,48,"X",controller.Review,true);
  Bind(controller,"messageOverlay",overlay.gameObject);Bind(controller,"painAlertIcon",Sprite("caution"));Bind(controller,"stressAlertIcon",Sprite("leaf"));overlay.gameObject.SetActive(false);
  PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Transitions/SceneTransition.prefab"),scene);
  var sourceScene=EditorSceneManager.OpenScene("Assets/Scenes/Settings.unity",OpenSceneMode.Additive);var sourceAudio=sourceScene.GetRootGameObjects().First(r=>r.name=="ScreenAudio");var audioGo=UnityEngine.Object.Instantiate(sourceAudio);audioGo.name="ScreenAudio";SceneManager.MoveGameObjectToScene(audioGo,scene);var audio=audioGo.GetComponent<UIScreenAudio>();EditorSceneManager.CloseScene(sourceScene,true);
  foreach(var root in scene.GetRootGameObjects()){
   foreach(var selectable in root.GetComponentsInChildren<Selectable>(true)){var sound=selectable.gameObject.AddComponent<UIClickSound>();Bind(sound,"screenAudio",audio);}
   foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)){var motion=text.gameObject.AddComponent<TextSlideIn>();var so=new SerializedObject(motion);so.FindProperty("fromOffset").vector2Value=new Vector2(-80,0);so.FindProperty("duration").floatValue=.95f;so.FindProperty("delay").floatValue=Mathf.Clamp01((430-text.rectTransform.position.y)/850)*.18f;so.ApplyModifiedPropertiesWithoutUndo();}
  }
  EditorSceneManager.SaveScene(scene,path);var scenes=EditorBuildSettings.scenes.ToList();scenes.Insert(1,new EditorBuildSettingsScene(path,true));EditorBuildSettings.scenes=scenes.ToArray();
  var gameplay=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();var config=GameplayEditorAccess.Owner(game,"performanceIndicators");var indicators=config.FindProperty("performanceIndicators");string[] names={"ScoreCard","AccuracyCard","Combo","ProgressTrack","RemainingTime"};indicators.arraySize=names.Length;for(int i=0;i<names.Length;i++)indicators.GetArrayElementAtIndex(i).objectReferenceValue=GameObject.Find("/Canvas/"+names[i]);config.FindProperty("wellbeingGameplayTip").objectReferenceValue=GameObject.Find("/Canvas/BottomBar/HoldingTip").GetComponent<TMP_Text>();config.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(gameplay);EditorSceneManager.SaveScene(gameplay);
  EditorSceneManager.OpenScene(path);return "Saved assessment scene with sliders, 0–10 ticks, messages, high thresholds 8, queued alerts, Skip/Continue, kit icons, menu audio and text entrances. Gameplay reduced-pressure UI references saved.";
 }
}
