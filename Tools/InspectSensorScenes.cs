using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class InspectSensorScenes
{
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before editing sensor scenes.");
  var old=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;var lines=new List<string>();
  foreach(var path in new[]{"Assets/Scenes/SensorConnection.unity","Assets/Scenes/SensorCalibration.unity"})
  {
   var scene=EditorSceneManager.OpenScene(path);lines.Add(path);
   foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
   {
    var txt=t.GetComponent<TMP_Text>();var b=t.GetComponent<UnityEngine.UI.Button>();var presenter=t.GetComponent<RhythmRehab.SensorFrameworkPresenter>();
    if(txt||b||presenter){string full=t.name;for(var p=t.parent;p;p=p.parent)full=p.name+"/"+full;var r=t as RectTransform;lines.Add(full+" | "+(r?r.anchoredPosition+" size="+r.sizeDelta:"")+" | "+(txt?txt.text:"")+(b?" BUTTON":"")+(presenter?" PRESENTER":""));}
   }
  }
  EditorSceneManager.OpenScene(old);lines.Add("APIs="+string.Join(",",Enum.GetNames(typeof(ApiCompatibilityLevel))));return string.Join("\n",lines);
 }
}
