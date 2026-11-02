using System;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
public static class InspectSettingsUI
{
 public static string Main()
 {
  var old=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string path=old.path;if(old.isDirty)EditorSceneManager.SaveScene(old);
  EditorSceneManager.OpenScene("Assets/Scenes/Settings.unity");var root=GameObject.Find("/Canvas").transform;
  string result=string.Join("\n",root.GetComponentsInChildren<RectTransform>(true).Where(r=>r.parent&&(r.parent==root||r.parent.name=="Audio")).Select(r=>r.name+" parent="+r.parent.name+" pos="+r.anchoredPosition+" size="+r.sizeDelta));
  result+="\nControllers: "+string.Join(",",UnityEngine.Object.FindObjectsByType<RhythmRehab.PrototypeSettings>().Select(c=>c.name));
  EditorSceneManager.OpenScene(path);return result;
 }
}
