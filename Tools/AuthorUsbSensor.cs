using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;

public static class AuthorUsbSensor
{
 const string Prefab="Assets/UI/Transitions/SceneTransition.prefab";
 static Transform Find(string path)=>GameObject.Find(path)?.transform??throw new Exception("Missing "+path);
 static TMP_Text Text(string path,string value,float size=26)
 {var t=Find(path).GetComponent<TMP_Text>();t.text=value;t.fontSize=size;t.enableAutoSizing=false;t.raycastTarget=false;return t;}
 static void Bind(UnityEngine.Object target,string property,UnityEngine.Object value)
 {var so=new SerializedObject(target);var p=so.FindProperty(property);if(p==null)throw new Exception("Missing field "+property);p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static void Set(Transform t,float x,float y,float w,float h)
 {var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
 static void Click(UnityEngine.UI.Button b,UnityAction action)
 {Undo.RecordObject(b,"Wire USB sensor button");for(int i=b.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(b.onClick,i);b.interactable=true;UnityEventTools.AddPersistentListener(b.onClick,action);EditorUtility.SetDirty(b);if(PrefabUtility.IsPartOfPrefabInstance(b))PrefabUtility.RecordPrefabInstancePropertyModifications(b);}
 static UnityEngine.UI.Button NewButton(Transform parent,string name,float x,float y,float w,string label,UnityAction action)
 {
  var t=parent.Find(name);GameObject go;
  if(t)go=t.gameObject;
  else {go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Space_Exploration_GUI_Kit/Prefabs/Buttons_Simple/Large/Blue/Long_Blue_Large_Button.prefab"));go.name=name;go.transform.SetParent(parent,false);}
  Set(go.transform,x,y,w,65);var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=go.GetComponent<UnityEngine.UI.Image>();button.targetGraphic.raycastTarget=true;button.targetGraphic.color=new Color(.22f,.5f,.8f);
  var text=go.GetComponentInChildren<TMP_Text>();text.text=label;text.fontSize=25;text.raycastTarget=false;text.color=new Color(.94f,.96f,1);
  if(!go.GetComponent<UIHoverFeedback>())go.AddComponent<UIHoverFeedback>();Click(button,action);return button;
 }
 static TMP_Text NewText(Transform parent,string name,float x,float y,float w,float h,string value,float size)
 {
  var t=parent.Find(name);var go=t?t.gameObject:new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));if(!t)go.transform.SetParent(parent,false);go.layer=5;Set(go.transform,x,y,w,h);
  var text=go.GetComponent<TMP_Text>();text.font=Find("/Canvas/Title").GetComponent<TMP_Text>().font;text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.9f,.96f,1);text.raycastTarget=false;return text;
 }
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before authoring sensor controls.");
  var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
  var root=PrefabUtility.LoadPrefabContents(Prefab);
  if(!root.GetComponent<UsbSensorConnection>())root.AddComponent<UsbSensorConnection>();
  PrefabUtility.SaveAsPrefabAsset(root,Prefab);PrefabUtility.UnloadPrefabContents(root);
  Connection();Calibration();
  var practice=EditorSceneManager.OpenScene("Assets/Scenes/RhythmCalibration.unity");
  Text("/Canvas/PracticeInfo/HowTo","1  Move the mouse or calibrated shin sensor.\n2  Press Space as a note meets the line.\n3  Adjust the sliders and try again.",24);
  EditorSceneManager.SaveScene(practice);
  AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(original);
  return "Authored COM-port selection, real USB status, two-position calibration and sensor/mouse selection. Shared connection added to the existing transition prefab.";
 }
 static void Connection()
 {
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/SensorConnection.unity");
  var controller=Find("/Controllers").GetComponent<SensorFrameworkPresenter>();
  Text("/Canvas/Subtitle","USB SHIN SENSOR  ·  Connect your board and check its live readings.",26);
  Bind(controller,"state",Text("/Canvas/ConnectionStatus/State","NOT CONNECTED",39));
  Set(Find("/Canvas/ConnectionStatus/State"),0,125,720,90);
  var left=Find("/Canvas/ConnectionStatus");foreach(Transform child in left)if(child.name=="Bluetooth")child.gameObject.SetActive(false);
  Bind(controller,"details",Text("/Canvas/ConnectionStatus/Details","Port     —\nDevice     —\nReadings     0\nControl     MOUSE + SPACE",27));
  Text("/Canvas/ConnectionStatus/Unavailable","Close Arduino Serial Monitor before connecting.",22);
  var connect=Find("/Canvas/ConnectionStatus/ConnectButton").GetComponent<UnityEngine.UI.Button>();Click(connect,controller.Connect);
  Bind(controller,"connectButton",connect);Bind(controller,"connectLabel",connect.GetComponentInChildren<TMP_Text>());connect.GetComponentInChildren<TMP_Text>().text="CONNECT USB";
  Text("/Canvas/AvailableDevices/Title","USB COM PORTS",29);
  Find("/Canvas/AvailableDevices/ScanButton").gameObject.SetActive(false);
  var empty=Find("/Canvas/AvailableDevices/EmptyDevices");
  foreach(Transform child in empty)if(child.name!="Text")child.gameObject.SetActive(false);
  var hint=Text("/Canvas/AvailableDevices/EmptyDevices/Text","Choose the port shown in\nArduino IDE → Tools → Port",29);Set(hint.transform,0,0,700,120);
  Bind(controller,"portLabel",NewText(Find("/Canvas/AvailableDevices"),"SelectedPort",0,-65,440,65,"NO COM PORTS",35));
  Bind(controller,"status",Text("/Canvas/AvailableDevices/FrameworkStatus","Connect your USB shin sensor to begin.",25));
  Text("/Canvas/AboutSensor/Text","USB SHIN SENSOR  ·  115200 BAUD\nAfter connecting, capture your resting and raised positions in SENSOR CALIBRATION.",25);
  EditorSceneManager.SaveScene(scene);
 }
 static void Calibration()
 {
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/SensorCalibration.unity");
  var controller=Find("/Controllers").GetComponent<SensorFrameworkPresenter>();var so=new SerializedObject(controller);so.FindProperty("calibrationPage").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
  Text("/Canvas/Subtitle","Set your movement range using the USB sensor attached to your shin.",26);
  Text("/Canvas/CalibrationSteps/Step0","01  CAPTURE REST",27);Text("/Canvas/CalibrationSteps/Step1","02  CAPTURE RAISED",27);Text("/Canvas/CalibrationSteps/Step2","03  USE SENSOR",27);
  Text("/Canvas/Instructions/Title","TWO POSITIONS",30);
  Text("/Canvas/Instructions/Instructions","1  Sit with your lower leg resting. Hold still and CAPTURE REST.\n\n2  Raise your lower leg to your chosen upper position. Hold still and CAPTURE RAISED.\n\n3  Select USE SENSOR.",25);
  Text("/Canvas/Instructions/Note","This is a movement reference, not a measured knee-joint angle.",22);
  Bind(controller,"state",Text("/Canvas/CalibrationStatus/Waiting","WAITING\nFOR SENSOR",37));
  Bind(controller,"details",Text("/Canvas/CalibrationStatus/Status","Connect the USB sensor first.",25));
  var capture=Find("/Canvas/CalibrationStatus/CalibrateButton").GetComponent<UnityEngine.UI.Button>();Click(capture,controller.ExplainCalibration);
  Bind(controller,"calibrateButton",capture);Bind(controller,"calibrateLabel",capture.GetComponentInChildren<TMP_Text>());capture.GetComponentInChildren<TMP_Text>().text="CAPTURE REST";capture.GetComponentInChildren<TMP_Text>().fontSize=24;
  var next=Find("/Canvas/NextButton").GetComponent<UnityEngine.UI.Button>();Click(next,controller.UseSensor);Bind(controller,"useSensorButton",next);next.GetComponentInChildren<TMP_Text>().text="USE SENSOR";
  NewButton(Find("/Canvas"),"UseMouseButton",0,-485,300,"USE MOUSE",controller.UseMouse);
  Bind(controller,"liveHeight",null);
  Bind(controller,"status",Text("/Canvas/FooterNote","Connect your USB shin sensor to begin.",23));
  var live=Find("/Canvas/FooterNote");Set(live,0,-415,1700,75);
  var oldMessage=Find("/Canvas").Find("CaptureMessage");if(oldMessage)oldMessage.gameObject.SetActive(false);
  foreach(var t in Find("/Canvas/SensorDiagram").GetComponentsInChildren<Transform>(true))if(t.name=="Bluetooth")t.gameObject.SetActive(false);
  EditorSceneManager.SaveScene(scene);
 }
}
