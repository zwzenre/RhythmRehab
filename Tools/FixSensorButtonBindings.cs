using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class FixSensorButtonBindings
{
 static void Wire(string path,UnityAction action)
 {
  var b=GameObject.Find(path).GetComponent<UnityEngine.UI.Button>();Undo.RecordObject(b,"Restore sensor click action");
  for(int i=b.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(b.onClick,i);
  UnityEventTools.AddPersistentListener(b.onClick,action);EditorUtility.SetDirty(b);
  if(PrefabUtility.IsPartOfPrefabInstance(b))PrefabUtility.RecordPrefabInstancePropertyModifications(b);
 }
 static void Verify(string path,string method)
 {
  var b=GameObject.Find(path).GetComponent<UnityEngine.UI.Button>();
  if(b.onClick.GetPersistentEventCount()!=1||b.onClick.GetPersistentTarget(0)==null||b.onClick.GetPersistentMethodName(0)!=method)
   throw new Exception("Click action not saved after scene reload: "+path);
 }
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode to save button bindings.");
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/SensorConnection.unity");var p=UnityEngine.Object.FindAnyObjectByType<SensorFrameworkPresenter>();
  Wire("/Canvas/ConnectionStatus/ConnectButton",p.Connect);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  EditorSceneManager.OpenScene("Assets/Scenes/SensorConnection.unity");
  Verify("/Canvas/ConnectionStatus/ConnectButton","Connect");
  scene=EditorSceneManager.OpenScene("Assets/Scenes/SensorCalibration.unity");p=UnityEngine.Object.FindAnyObjectByType<SensorFrameworkPresenter>();
  Wire("/Canvas/CalibrationStatus/CalibrateButton",p.ExplainCalibration);Wire("/Canvas/NextButton",p.UseSensor);Wire("/Canvas/UseMouseButton",p.UseMouse);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);EditorSceneManager.OpenScene("Assets/Scenes/SensorCalibration.unity");
  Verify("/Canvas/CalibrationStatus/CalibrateButton","ExplainCalibration");Verify("/Canvas/NextButton","UseSensor");Verify("/Canvas/UseMouseButton","UseMouse");
  EditorSceneManager.OpenScene("Assets/Scenes/SensorConnection.unity");return "PASS: sensor button actions persisted after saving and reopening both scenes.";
 }
}
