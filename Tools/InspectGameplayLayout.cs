using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class InspectGameplayLayout
{
 public static string Main()
 {
  var canvas=GameObject.Find("/Canvas");
  return "playing="+EditorApplication.isPlaying+" scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+" dirty="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty+"\n"+
   string.Join("\n",canvas.GetComponentsInChildren<RectTransform>(true).Where(t=>t.parent&&(t.parent==canvas.transform||t.parent.name=="Sidebar"||t.parent.name=="Playfield"||t.parent.name=="ProgressTrack")).Select(t=>t.name+" parent="+t.parent.name+" pos="+t.anchoredPosition+" size="+t.sizeDelta+" anchors="+t.anchorMin+"/"+t.anchorMax+ (t.GetComponent<TMPro.TMP_Text>()?" text="+t.GetComponent<TMPro.TMP_Text>().text:"")));
 }
}

