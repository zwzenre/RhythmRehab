using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class PolishCalibrationSettings
{
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new System.Exception("Exit Play mode first.");
  string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
  EditorSceneManager.OpenScene("Assets/Scenes/Settings.unity");
  var title=GameObject.Find("/Canvas/SystemTitle").GetComponent<RectTransform>();title.anchoredPosition=new Vector2(190,0);title.sizeDelta=new Vector2(1240,45);
  GameObject.Find("/Canvas/SettingsSidebar/Tip").GetComponent<TMP_Text>().text="TRY IT FIRST\n\nTest timing and note speed in the rhythm preview.\n\nSensor tools await firmware.";
  foreach(var slider in UnityEngine.Object.FindObjectsByType<Slider>())
  {
   foreach(var r in slider.GetComponentsInChildren<RectTransform>())if(r!=slider.transform&&r.sizeDelta.x>500){var size=r.sizeDelta;size.x*=640f/760;r.sizeDelta=size;}
   if(slider.handleRect){var size=slider.handleRect.sizeDelta;size.x=24;size.y=-6;slider.handleRect.sizeDelta=size;}
  }
  EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  EditorSceneManager.OpenScene("Assets/Scenes/RhythmCalibration.unity");
  foreach(var slider in UnityEngine.Object.FindObjectsByType<Slider>())if(slider.handleRect){var size=slider.handleRect.sizeDelta;size.y=-6;slider.handleRect.sizeDelta=size;}
  EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  EditorSceneManager.OpenScene(original);return "Settings heading, sidebar text and slider-handle spacing polished.";
 }
}
