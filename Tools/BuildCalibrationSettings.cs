using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;

public static class BuildCalibrationSettings
{
 const string Kit="Assets/Assets/Space_Exploration_GUI_Kit/";
 static readonly Color White=new Color(.94f,.96f,1),Muted=new Color(.57f,.64f,.79f),Purple=new Color(.63f,.3f,1),Cyan=new Color(.12f,.85f,1),PanelColor=new Color(.03f,.04f,.09f,.97f);
 static TMP_FontAsset font;static GameObject backdrop;
 static GameObject Node(Transform parent,string name,float x,float y,float w,float h)
 {
  var old=parent?parent.Find(name):null;var go=old?old.gameObject:new GameObject(name,typeof(RectTransform));
  if(!old){Undo.RegisterCreatedObjectUndo(go,"Build settings UI");if(parent)go.transform.SetParent(parent,false);}
  go.layer=5;Rect((RectTransform)go.transform,x,y,w,h);return go;
 }
 static void Rect(RectTransform r,float x,float y,float w,float h)
 {Undo.RecordObject(r,"Layout settings UI");r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
 static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
 static T Add<T>(GameObject go)where T:Component
 {var component=go.GetComponent<T>();return component?component:go.AddComponent<T>();}
 static GameObject Panel(Transform p,string n,float x,float y,float w,float h,Color? color=null)
 {var go=Node(p,n,x,y,w,h);var image=Add<Image>(go);image.color=color??PanelColor;image.raycastTarget=false;return go;}
 static TMP_Text Text(Transform p,string n,float x,float y,float w,float h,string value,float size=26,Color? color=null,bool center=false)
 {
  var t=Add<TextMeshProUGUI>(Node(p,n,x,y,w,h));t.font=font;t.fontSharedMaterial=font.material;t.text=value;t.fontSize=size;t.enableAutoSizing=false;
  t.color=color??White;t.alignment=center?TextAlignmentOptions.Center:TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;
  t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Ellipsis;return t;
 }
 static Image Icon(Transform p,string n,string file,float x,float y,float size,Color color)
 {
  var sprite=AssetDatabase.LoadAllAssetsAtPath(Kit+"Picto_Icons/White/"+file+"-128.png").OfType<Sprite>().OrderByDescending(s=>s.rect.width*s.rect.height).First();
  var image=Add<Image>(Node(p,n,x,y,size,size));image.sprite=sprite;image.color=color;image.preserveAspect=true;image.raycastTarget=false;return image;
 }
 static Button Button(Transform p,string n,float x,float y,float w,float h,string value,UnityAction callback=null,bool blue=false)
 {
  var old=p.Find(n);GameObject go;
  if(old)go=old.gameObject;else{string c=blue?"Blue":"Purple";go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kit+"Prefabs/Buttons_Simple/Large/"+c+"/Long_"+c+"_Large_Button.prefab"));go.name=n;go.transform.SetParent(p,false);}
  Rect((RectTransform)go.transform,x,y,w,h);var image=go.GetComponent<Image>();image.color=blue?new Color(.22f,.5f,.8f):new Color(.55f,.2f,.95f);
  var b=go.GetComponent<Button>();b.targetGraphic=image;image.raycastTarget=true;
  var label=go.GetComponentInChildren<TMP_Text>();label.text=value;label.fontSize=28;label.enableAutoSizing=false;label.color=White;label.raycastTarget=false;
  if(callback!=null)Click(b,callback);return b;
 }
 static void Click(Button b,UnityAction action)
 {
  for(int i=b.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(b.onClick,i);
  UnityEventTools.AddPersistentListener(b.onClick,action);EditorUtility.SetDirty(b);
 }
 static void Bind(MonoBehaviour target,string property,UnityEngine.Object value)
 {var so=new SerializedObject(target);so.FindProperty(property).objectReferenceValue=value;so.ApplyModifiedProperties();}
 static Slider Slider(Transform p,string n,float x,float y,float width,float min,float max,float value,bool whole=false)
 {
  var go=Node(p,n,x,y,width,42);var slider=Add<Slider>(go);slider.direction=UnityEngine.UI.Slider.Direction.LeftToRight;
  var track=Panel(go.transform,"Track",0,0,width-30,8,new Color(.15f,.12f,.28f));
  var fill=Panel(track.transform,"Fill",0,0,width-30,8,Purple);Stretch((RectTransform)fill.transform);
  var area=Node(go.transform,"HandleArea",0,0,width-30,42);
  var handle=Panel(area.transform,"Handle",0,0,24,36,new Color(.75f,.5f,1));handle.GetComponent<Image>().raycastTarget=true;
  slider.fillRect=(RectTransform)fill.transform;slider.handleRect=(RectTransform)handle.transform;slider.targetGraphic=handle.GetComponent<Image>();
  slider.minValue=min;slider.maxValue=max;slider.wholeNumbers=whole;slider.SetValueWithoutNotify(value);
  var hit=Add<Image>(go);hit.color=Color.clear;hit.raycastTarget=true;return slider;
 }
 static Transform NewPage(string name,string title,string subtitle,out SceneNavigation nav)
 {
  string path="Assets/Scenes/"+name+".unity";if(File.Exists(path))throw new Exception("Scene already exists; edit it in place: "+path);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);
  var root=Node(null,"Canvas",0,0,1920,1080);Add<Canvas>(root).renderMode=RenderMode.ScreenSpaceOverlay;Add<GraphicRaycaster>(root);
  var scaler=Add<CanvasScaler>(root);scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
  var bg=(GameObject)PrefabUtility.InstantiatePrefab(backdrop,scene);bg.transform.SetParent(root.transform,false);Stretch((RectTransform)bg.transform);
  var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
  var controllers=new GameObject("Controllers");nav=controllers.AddComponent<SceneNavigation>();
  Text(root.transform,"Title",0,443,1400,85,title,52,null,true);Text(root.transform,"Subtitle",0,365,1620,60,subtitle,26,Muted,true);
  Button(root.transform,"BackButton",-820,450,180,70,"BACK",nav.Settings,true);Button(root.transform,"HomeButton",820,450,180,70,"HOME",nav.Home,true);
  PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Transitions/SceneTransition.prefab"),scene);
  EditorSceneManager.SaveScene(scene,path);
  EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(path,true)}).ToArray();
  return root.transform;
 }
 static void LinkRow(Transform p,string n,float y,string title,string detail,string icon,Color accent,UnityAction action)
 {
  var row=Panel(p,n,190,y,1340,100);var image=row.GetComponent<Image>();image.raycastTarget=true;var b=Add<Button>(row);b.targetGraphic=image;Click(b,action);
  Icon(row.transform,"Icon",icon,-580,0,46,accent);Text(row.transform,"Title",-30,19,1010,43,title,31);
  Text(row.transform,"Detail",-30,-24,1010,40,detail,24,Muted);Icon(row.transform,"Arrow","right-arrow",600,0,28,accent);
 }
 static void Hub()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/Settings.unity");var c=GameObject.Find("/Canvas").transform;var nav=GameObject.Find("/Controllers").GetComponent<SceneNavigation>();
  foreach(string old in new[]{"Controls","Sensor"}){var t=c.Find(old);if(t)t.gameObject.SetActive(false);}
  var title=Text(c,"Title",0,443,1240,90,"SETTINGS",58,null,true);
  Text(c,"Subtitle",0,365,1500,50,"Tune your rhythm and prepare your sensor.",27,Muted,true);
  Button(c,"BackButton",-810,450,200,75,"BACK",nav.Home,true);Button(c,"HomeButton",810,450,200,75,"HOME",nav.Home,true);
  var audio=c.Find("Audio");Rect((RectTransform)audio,190,180,1340,250);
  Text(audio,"Title",-440,90,400,45,"AUDIO",29,Purple);
  Rect((RectTransform)audio.Find("BeatLabel"),-460,20,280,45);audio.Find("BeatLabel").GetComponent<TMP_Text>().fontSize=26;
  Rect((RectTransform)audio.Find("BeatSlider"),100,20,640,40);Rect((RectTransform)audio.Find("BeatValue"),540,20,140,45);
  Rect((RectTransform)audio.Find("SfxLabel"),-460,-65,280,45);audio.Find("SfxLabel").GetComponent<TMP_Text>().fontSize=26;
  Rect((RectTransform)audio.Find("SfxSlider"),100,-65,640,40);Rect((RectTransform)audio.Find("SfxValue"),540,-65,140,45);
  Text(c,"SystemTitle",-230,0,800,45,"SYSTEM & CALIBRATION",26,Cyan);
  LinkRow(c,"RhythmRow",-90,"Rhythm Calibration","Try a practice beat. Adjust judgement offset and note speed.","music",Purple,nav.RhythmCalibration);
  LinkRow(c,"CalibrationRow",-220,"Sensor Calibration","Prepare a reference position. Hardware integration pending.","settings",Cyan,nav.SensorCalibration);
  LinkRow(c,"ConnectionRow",-350,"Sensor Connection","Connection framework for your wearable shin sensor.","bluetooth",Cyan,nav.SensorConnection);
  var side=Panel(c,"SettingsSidebar",-720,-80,340,770);
  Text(side.transform,"Category",0,315,290,60,"SETTINGS",27,Muted,true);
  Button(side.transform,"AudioTab",0,220,300,70,"AUDIO",nav.Settings);
  Button(side.transform,"RhythmTab",0,115,300,70,"RHYTHM CALIBRATION",nav.RhythmCalibration,true);
  Button(side.transform,"CalibrationTab",0,10,300,70,"SENSOR CALIBRATION",nav.SensorCalibration,true);
  Button(side.transform,"ConnectionTab",0,-95,300,70,"SENSOR CONNECTION",nav.SensorConnection,true);
  Text(side.transform,"Tip",0,-270,280,210,"TRY IT FIRST\n\nUse the rhythm preview to find a comfortable timing and speed.\n\nSensor pages are framework-only.",23,Muted);
  Text(c,"Footer",0,-500,1740,55,"Audio saves automatically. Rhythm settings apply after you press SAVE.",23,Muted,true);
  EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
 }
 static void Rhythm()
 {
  var c=NewPage("RhythmCalibration","RHYTHM CALIBRATION","Try the beat with mouse + Space. Tune timing and speed before you play.",out var nav);
  var controller=GameObject.Find("/Controllers").AddComponent<RhythmCalibrationController>();
  var preview=Panel(c,"PreviewPanel",-300,50,1180,520);Text(preview.transform,"Title",0,215,1100,50,"PRACTICE FIELD  ·  NOTES MOVE RIGHT TO LEFT",26,Purple,true);
  var field=Panel(preview.transform,"Playfield",0,-20,1080,350,new Color(.005f,.025f,.045f));Bind(controller,"playfield",field.transform);
  Color[] colors={new Color(.3f,1,.45f),new Color(1,.75f,.15f),new Color(1,.3f,.5f)};
  for(int i=0;i<3;i++){Panel(field.transform,"Lane"+i,0,(i-1)*110,1040,2,colors[i]);Text(field.transform,"Label"+i,-460,(i-1)*110+30,140,40,new[]{"LOW","MID","HIGH"}[i],22,colors[i],true);}
  Panel(field.transform,"JudgementLine",-350,0,4,320,Purple);
  var disc=AssetDatabase.LoadAllAssetsAtPath(Kit+"Picto_Icons/White/full-moon-128.png").OfType<Sprite>().OrderByDescending(s=>s.rect.width*s.rect.height).First();
  var marker=Panel(field.transform,"Marker",-350,-110,44,44,colors[0]);marker.GetComponent<Image>().sprite=disc;Bind(controller,"marker",marker.transform);
  var so=new SerializedObject(controller);var views=so.FindProperty("notes");var images=so.FindProperty("noteImages");views.arraySize=images.arraySize=8;
  for(int i=0;i<8;i++){var note=Panel(field.transform,"Note"+i,450,0,30,30,colors[0]);note.GetComponent<Image>().sprite=disc;views.GetArrayElementAtIndex(i).objectReferenceValue=note.transform;images.GetArrayElementAtIndex(i).objectReferenceValue=note.GetComponent<Image>();note.SetActive(false);}so.ApplyModifiedProperties();
  Bind(controller,"feedback",Text(preview.transform,"Feedback",0,-230,1090,45,"PRESS START TO TRY THE RHYTHM",26,null,true));
  var side=Panel(c,"PracticeInfo",610,50,540,520);Text(side.transform,"Title",0,205,470,60,"120 BPM TEMPO DEMO",30,Cyan,true);
  Text(side.transform,"HowTo",0,95,470,150,"1  Move the mouse to match a note.\n2  Press Space as it meets the line.\n3  Adjust the sliders and try again.",26);
  Bind(controller,"statistics",Text(side.transform,"Statistics",0,-35,470,110,"No practice hits yet.",26,Muted,true));
  var start=Button(side.transform,"StartButton",0,-155,440,70,"START PREVIEW",controller.TogglePreview);Bind(controller,"startLabel",start.GetComponentInChildren<TMP_Text>());
  Text(side.transform,"NoHistory",0,-225,470,40,"Practice is not saved to session history.",21,Muted,true);
  var controls=Panel(c,"TimingControls",0,-335,1760,200);
  Text(controls.transform,"OffsetLabel",-640,50,360,40,"JUDGEMENT OFFSET",26,Purple);
  Text(controls.transform,"OffsetHint",-640,10,360,36,"Positive = later · Negative = earlier",19,Muted);
  Text(controls.transform,"SpeedLabel",-640,-42,360,40,"NOTE SPEED",26,Cyan);
  Text(controls.transform,"SpeedHint",-640,-76,360,30,"Travel speed only; BPM stays the same",18,Muted);
  var offset=Slider(controls.transform,"OffsetSlider",150,48,900,-250,250,0,true);Bind(controller,"offsetSlider",offset);UnityEventTools.AddPersistentListener(offset.onValueChanged,controller.Adjust);
  var speed=Slider(controls.transform,"SpeedSlider",150,-43,900,.5f,2,1);Bind(controller,"speedSlider",speed);UnityEventTools.AddPersistentListener(speed.onValueChanged,controller.Adjust);
  Bind(controller,"offsetValue",Text(controls.transform,"OffsetValue",735,48,160,50,"0 ms",30,null,true));Bind(controller,"speedValue",Text(controls.transform,"SpeedValue",735,-43,160,50,"1.00x",30,null,true));
  Button(c,"ResetButton",-740,-485,280,65,"RESET VALUES",controller.ResetValues,true);
  Bind(controller,"saveStatus",Text(c,"SaveStatus",-30,-485,1110,70,"Adjust, test, then SAVE.",21,Muted,true));
  Button(c,"SaveButton",700,-485,300,65,"SAVE",controller.Save);
  var audio=new GameObject("PreviewMetronome",typeof(AudioSource));audio.transform.SetParent(controller.transform,false);audio.GetComponent<AudioSource>().playOnAwake=false;Bind(controller,"metronome",audio.GetComponent<AudioSource>());
  Click(GameObject.Find("/Canvas/BackButton").GetComponent<Button>(),controller.Back);Click(GameObject.Find("/Canvas/HomeButton").GetComponent<Button>(),controller.Home);
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
 }
 static void Connection()
 {
  var c=NewPage("SensorConnection","SENSOR CONNECTION","FRAMEWORK ONLY · Firmware and Bluetooth integration are not connected yet.",out var nav);
  var controller=GameObject.Find("/Controllers").AddComponent<SensorFrameworkPresenter>();
  var left=Panel(c,"ConnectionStatus",-440,-25,820,590);Text(left.transform,"Title",0,235,720,55,"CONNECTION STATUS",30,Purple);
  Icon(left.transform,"Bluetooth","bluetooth",-260,120,85,Cyan);Text(left.transform,"State",80,125,490,90,"NOT CONNECTED",38,Muted);
  Text(left.transform,"Details",0,-10,660,170,"Device name       —\nBattery                 —\nSignal strength     —",29,Muted);
  var connect=Button(left.transform,"ConnectButton",0,-175,660,70,"CONNECT SENSOR");connect.interactable=false;
  Text(left.transform,"Unavailable",0,-245,720,50,"Controls will be enabled when real hardware is integrated.",22,Muted,true);
  var right=Panel(c,"AvailableDevices",450,-25,860,590);Text(right.transform,"Title",-110,235,550,60,"AVAILABLE DEVICES",29,Cyan);
  Button(right.transform,"ScanButton",290,235,210,65,"SCAN",controller.Scan,true);
  var empty=Panel(right.transform,"EmptyDevices",0,55,760,205,new Color(.05f,.065f,.13f));Icon(empty.transform,"Device","bluetooth",-260,0,74,Muted);
  Text(empty.transform,"Text",75,0,540,150,"NO LIVE DEVICE DATA\nFirmware is not connected.",29,Muted,true);
  Bind(controller,"status",Text(right.transform,"FrameworkStatus",0,-165,750,160,"The device list, scan action and connection controls are ready for future hardware integration.\nNo device connection is simulated.",25,Muted));
  var foot=Panel(c,"AboutSensor",0,-430,1760,140);Text(foot.transform,"Text",-220,0,1180,115,"WEARABLE SHIN SENSOR\nThis page will show live device status, battery and signal strength. Use mouse + Space while hardware integration is pending.",25,Muted);
  Button(foot.transform,"CalibrationButton",640,0,420,70,"SENSOR CALIBRATION",nav.SensorCalibration);
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
 }
 static void Calibration()
 {
  var c=NewPage("SensorCalibration","SENSOR CALIBRATION","FRAMEWORK ONLY · No sensor measurements or reference angles are recorded yet.",out var nav);
  var controller=GameObject.Find("/Controllers").AddComponent<SensorFrameworkPresenter>();
  var steps=Panel(c,"CalibrationSteps",0,265,1760,70);
  for(int i=0;i<3;i++)Text(steps.transform,"Step"+i,(i-1)*565,0,530,65,new[]{"01  INITIAL POSITION","02  CAPTURE REFERENCE","03  COMPLETE"}[i],26,i==0?Purple:Muted,true);
  var left=Panel(c,"Instructions",-620,-80,500,590);Text(left.transform,"Title",0,235,440,60,"INITIAL POSITION",30,Purple);
  Text(left.transform,"Instructions",0,15,420,350,"1  Sit in your starting position.\n\n2  Attach the sensor securely to your shin.\n\n3  Keep still while reference data is captured.",28);
  Text(left.transform,"Note",0,-230,420,100,"The capture step will be enabled after a live sensor is connected.",23,Muted);
  var diagram=Panel(c,"SensorDiagram",45,-80,780,590);Text(diagram.transform,"Title",0,235,720,55,"SHIN SENSOR REFERENCE",28,Cyan,true);
  Color line=new Color(.46f,.25f,.8f);
  Panel(diagram.transform,"Torso",-130,65,6,180,line);Panel(diagram.transform,"Thigh",-40,-25,180,6,line);Panel(diagram.transform,"Shin",50,-125,6,200,line);Panel(diagram.transform,"Foot",95,-225,95,6,line);
  var disc=AssetDatabase.LoadAllAssetsAtPath(Kit+"Picto_Icons/White/full-moon-128.png").OfType<Sprite>().OrderByDescending(s=>s.rect.width*s.rect.height).First();
  var head=Panel(diagram.transform,"Head",-130,190,60,60,line);head.GetComponent<Image>().sprite=disc;
  Panel(diagram.transform,"Seat",-110,-40,135,5,new Color(.18f,.25f,.4f));Panel(diagram.transform,"ChairLeg",-175,-125,5,170,new Color(.18f,.25f,.4f));
  var sensor=Panel(diagram.transform,"Sensor",50,-125,36,60,new Color(.07f,.32f,.38f));Icon(sensor.transform,"Bluetooth","bluetooth",0,0,25,Cyan);
  Text(diagram.transform,"SensorLabel",205,-125,230,100,"SENSOR\nShin position",25,Cyan,true);
  var status=Panel(c,"CalibrationStatus",670,-80,420,590);Text(status.transform,"Title",0,235,360,70,"CALIBRATION STATUS",27,Purple,true);
  Text(status.transform,"Waiting",0,105,360,140,"WAITING\nFOR SENSOR",36,Muted,true);
  Bind(controller,"status",Text(status.transform,"Status",0,-45,350,150,"Not calibrated.\nNo reference position recorded.",26,Muted,true));
  var start=Button(status.transform,"CalibrateButton",0,-205,350,70,"CALIBRATE",controller.ExplainCalibration);start.interactable=false;
  Text(c,"FooterNote",-150,-425,1400,65,"The starting position will be used as the reference once sensor support is available.",23,Muted);
  Button(c,"CancelButton",-650,-485,390,65,"BACK TO SETTINGS",nav.Settings,true);var next=Button(c,"NextButton",650,-485,390,65,"NEXT");next.interactable=false;
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
 }
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
  var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string original=string.IsNullOrEmpty(current.path)?"Assets/Scenes/Settings.unity":current.path;if(current.isDirty&&!string.IsNullOrEmpty(current.path))EditorSceneManager.SaveScene(current);
  font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Kit+"Fonts/Asap-Bold SDF.asset");if(!font)throw new Exception("GUI-kit font missing.");
  EditorSceneManager.OpenScene("Assets/Scenes/Settings.unity");
  if(!AssetDatabase.IsValidFolder("Assets/UI/Settings"))AssetDatabase.CreateFolder("Assets/UI","Settings");
  backdrop=PrefabUtility.SaveAsPrefabAsset(GameObject.Find("/Canvas/Background"),"Assets/UI/Settings/SettingsBackdrop.prefab");
  Hub();Rhythm();Connection();Calibration();AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(original);
  return "Settings hub updated; RhythmCalibration, SensorConnection and SensorCalibration scenes saved and added to Build Settings. All UI objects and callbacks authored in the Inspector.";
 }
}
