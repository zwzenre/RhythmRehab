using System;
using UnityEngine;
using UnityEditor.SceneManagement;
using TMPro;
public static class UpdateFreeHeightUI
{
 static void Set(string path,string text)
 {
  var go=GameObject.Find(path);if(!go||!go.GetComponent<TMP_Text>())throw new Exception("Missing label "+path);
  UnityEditor.Undo.RecordObject(go.GetComponent<TMP_Text>(),"Describe continuous Free Mode heights");
  go.GetComponent<TMP_Text>().text=text;UnityEditor.EditorUtility.SetDirty(go.GetComponent<TMP_Text>());
 }
 public static string Main()
 {
  if(UnityEditor.EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
  var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string originalPath=original.path;
  if(original.isDirty)EditorSceneManager.SaveScene(original); // Preserve current Inspector edits.
  EditorSceneManager.OpenScene("Assets/Scenes/ModeSelection.unity");
  Set("/Canvas/FreeCard/Description","Varied heights between guide lines\nChoose EASY, MEDIUM or HARD\nFollow a changing rhythm pattern");
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  EditorSceneManager.OpenScene("Assets/Scenes/DifficultySelection.unity");
  Set("/Canvas/Choice0/Description","16 notes\nHeights from LOW to MID\nWider timing window");
  Set("/Canvas/Choice1/Description","24 notes\nHeights across the full range\nStandard timing window");
  Set("/Canvas/Choice2/Description","32 notes\nLarger continuous height changes\nTighter timing window");
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  EditorSceneManager.OpenScene(originalPath);return "Free Mode and difficulty labels updated; current scene edits preserved.";
 }
}
