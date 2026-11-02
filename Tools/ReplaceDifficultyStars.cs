using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// Editor authoring only: the stars are saved Images, editable in the scene Inspector.
public static class ReplaceDifficultyStars
{
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before authoring.");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.name!="DifficultySelection")throw new Exception("Open DifficultySelection first.");
  const string asset="Assets/Assets/Space_Exploration_GUI_Kit/Icons/star-128.png";
  var sprite=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<Sprite>().OrderByDescending(s=>s.rect.width*s.rect.height).FirstOrDefault();
  if(!sprite)throw new Exception("The package's star sprite is missing.");
  int count=0;
  for(int i=0;i<3;i++)
  {
   var card=GameObject.Find("/Canvas/Choice"+i);if(!card)throw new Exception("Missing difficulty card "+i);
   var level=card.transform.Find("Level");if(!level||!level.GetComponent<TMP_Text>())throw new Exception("Missing rating label.");
   Undo.RecordObject(level.gameObject,"Replace rating asterisks with star icons");level.gameObject.SetActive(false);
   var found=card.transform.Find("DifficultyStars");
   var group=found?found.gameObject:new GameObject("DifficultyStars",typeof(RectTransform),typeof(HorizontalLayoutGroup));
   if(!found){Undo.RegisterCreatedObjectUndo(group,"Add difficulty star row");group.transform.SetParent(card.transform,false);}
   group.layer=5;var rect=(RectTransform)group.transform;Undo.RecordObject(rect,"Centre rating row");
   rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.pivot=Vector2.one*.5f;
   rect.anchoredPosition=((RectTransform)level).anchoredPosition;rect.sizeDelta=((RectTransform)level).sizeDelta;
   var layout=group.GetComponent<HorizontalLayoutGroup>();layout.childAlignment=TextAnchor.MiddleCenter;
   layout.spacing=12;layout.childControlWidth=false;layout.childControlHeight=false;layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;
   for(int j=0;j<=i;j++)
   {
    var existing=group.transform.Find("Star"+(j+1));var go=existing?existing.gameObject:new GameObject("Star"+(j+1),typeof(RectTransform),typeof(Image));
    if(!existing){Undo.RegisterCreatedObjectUndo(go,"Place rating star");go.transform.SetParent(group.transform,false);}
    go.layer=5;var r=(RectTransform)go.transform;r.sizeDelta=new Vector2(48,48);r.localScale=Vector3.one;
    var image=go.GetComponent<Image>();Undo.RecordObject(image,"Set package star icon");image.sprite=sprite;image.color=Color.white;image.preserveAspect=true;image.raycastTarget=false;
    EditorUtility.SetDirty(image);count++;
   }
   EditorUtility.SetDirty(layout);Canvas.ForceUpdateCanvases();
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  return count+" gold GUI-kit star Images saved: Easy 1, Medium 2, Hard 3. Original text labels hidden.";
 }
}
