using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class InspectSceneBackgrounds
{
 public static string Main()
 {
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene changes first.");
  var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
  var lines=new System.Collections.Generic.List<string>();
  foreach(var path in System.IO.Directory.GetFiles("Assets/Scenes","*.unity"))
  {
   var scene=EditorSceneManager.OpenScene(path);
   foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas))
   {
    lines.Add(scene.name+" CANVAS "+canvas.name+" children: "+string.Join(",",canvas.transform.Cast<Transform>().Select(t=>t.name)));
    foreach(var g in canvas.GetComponentsInChildren<Graphic>(true).Where(g=>g.name=="Background"||g.name=="SpaceBackdrop"||g.name=="MidnightTint"))
    {
     var rt=g.rectTransform;var raw=g as RawImage;
     lines.Add("  "+g.name+" "+g.GetType().Name+" color "+g.color+" size "+rt.sizeDelta+" texture "+(raw?AssetDatabase.GetAssetPath(raw.texture):""));
    }
   }
  }
  EditorSceneManager.OpenScene(original);return string.Join("\n",lines);
 }
}
