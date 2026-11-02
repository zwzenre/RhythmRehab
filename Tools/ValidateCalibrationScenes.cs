using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class ValidateCalibrationScenes
{
 public static string Main()
 {
  string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;int references=0;
  foreach(var name in new[]{"Settings","RhythmCalibration","SensorConnection","SensorCalibration"})
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
   if(UnityEngine.Object.FindObjectsByType<EventSystem>().Length!=1)throw new Exception("EventSystem count in "+name);
   if(UnityEngine.Object.FindObjectsByType<IrisSceneTransition>().Length!=1)throw new Exception("Iris missing in "+name);
   foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>().Where(c=>c is RhythmCalibrationController||c is SensorFrameworkPresenter||c is PrototypeSettings))
   {
    var it=new SerializedObject(c).GetIterator();while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference)
    {if(it.objectReferenceValue==null)throw new Exception(name+" missing "+it.propertyPath);references++;}
   }
   if(EditorBuildSettings.scenes.Count(s=>s.enabled&&s.path==scene.path)!=1)throw new Exception("Build entry missing: "+name);
  }
  EditorSceneManager.OpenScene(original);return references+" Inspector references checked; all four pages have one EventSystem, an iris transition and an enabled build entry.";
 }
}
