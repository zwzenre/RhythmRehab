using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class ExpandRhythmPreviewSlots
{
 public static string Main()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/RhythmCalibration.unity");
  var c=UnityEngine.Object.FindAnyObjectByType<RhythmCalibrationController>();var so=new SerializedObject(c);
  var field=GameObject.Find("/Canvas/PreviewPanel/Playfield").transform;
  var views=so.FindProperty("notes");var images=so.FindProperty("noteImages");views.arraySize=images.arraySize=8;
  for(int i=0;i<8;i++)
  {
   var t=field.Find("Note"+i);
   if(!t){var go=UnityEngine.Object.Instantiate(field.Find("Note0").gameObject,field,false);go.name="Note"+i;Undo.RegisterCreatedObjectUndo(go,"Add slow-speed preview capacity");t=go.transform;}
   t.gameObject.SetActive(false);views.GetArrayElementAtIndex(i).objectReferenceValue=t;images.GetArrayElementAtIndex(i).objectReferenceValue=t.GetComponent<Image>();
  }
  so.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  return "Eight saved note slots support full travel at the slowest 0.5x preview speed.";
 }
}
