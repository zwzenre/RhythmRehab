using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using RhythmRehab;
public static class AuthorCalibrationHumanModel
{
 const string Target="Assets/Scenes/SensorCalibration.unity";
 static void Rect(RectTransform rect,Vector2 position,Vector2 size)
 {rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;}
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before editing the calibration scene.");
  if(SceneManager.GetActiveScene().isDirty)throw new Exception("The open scene has unsaved changes.");
  var gameplay=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
  var original=gameplay.GetRootGameObjects().Single(g=>g.name=="HumanLegTrackerStage");
  var scene=EditorSceneManager.OpenScene(Target,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
  var stage=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="CalibrationHumanStage");
  if(!stage){stage=UnityEngine.Object.Instantiate(original);stage.name="CalibrationHumanStage";SceneManager.MoveGameObjectToScene(stage,scene);}
  stage.transform.position=new Vector3(120,0,0);
  var tracker=stage.GetComponent<HumanLegTracker>();var config=new SerializedObject(tracker);
  config.FindProperty("gameplay").objectReferenceValue=null;config.FindProperty("sensorCalibrationPreview").boolValue=true;
  config.FindProperty("maximumVisualAngle").floatValue=30;
  var panel=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").transform.Find("SensorDiagram");
  if(!panel)throw new Exception("Calibration illustration panel missing.");
  foreach(Transform child in panel)if(child.name!="Title"&&child.name!="HumanModelView"&&child.name!="ModelCaption")child.gameObject.SetActive(false);
  var viewObject=panel.Find("HumanModelView")?.gameObject;
  if(!viewObject){viewObject=new GameObject("HumanModelView",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));viewObject.transform.SetParent(panel,false);viewObject.layer=5;}
  Rect((RectTransform)viewObject.transform,new Vector2(0,-25),new Vector2(700,440));
  var view=viewObject.GetComponent<UnityEngine.UI.RawImage>();view.raycastTarget=false;view.uvRect=new Rect(1,0,-1,1);view.color=Color.white;
  const string texturePath="Assets/UI/LegTracker/CalibrationLegView.renderTexture";
  var texture=AssetDatabase.LoadAssetAtPath<RenderTexture>(texturePath);
  if(!texture){texture=new RenderTexture(1120,704,24,RenderTextureFormat.ARGB32);texture.name="CalibrationLegView";AssetDatabase.CreateAsset(texture,texturePath);}
  view.texture=texture;
  var camera=stage.GetComponentInChildren<Camera>(true);camera.targetTexture=texture;camera.orthographic=true;camera.orthographicSize=.9f;
  camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<30;
  foreach(var root in scene.GetRootGameObjects())foreach(var other in root.GetComponentsInChildren<Camera>(true))if(other!=camera)other.cullingMask&=~(1<<30);
  var captionObject=panel.Find("ModelCaption")?.gameObject;
  if(!captionObject){captionObject=new GameObject("ModelCaption",typeof(RectTransform),typeof(TextMeshProUGUI));captionObject.transform.SetParent(panel,false);captionObject.layer=5;}
  Rect((RectTransform)captionObject.transform,new Vector2(0,-265),new Vector2(700,45));
  var caption=captionObject.GetComponent<TMP_Text>();caption.font=panel.Find("Title").GetComponent<TMP_Text>().font;
  caption.text="RESTING POSITION";caption.fontSize=24;caption.alignment=TextAlignmentOptions.Center;caption.color=new Color(.15f,.85f,1);caption.raycastTarget=false;
  config.FindProperty("calibrationCaption").objectReferenceValue=caption;config.ApplyModifiedPropertiesWithoutUndo();
  panel.Find("Title").GetComponent<TMP_Text>().text="SHIN MOVEMENT REFERENCE";
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(gameplay,true);AssetDatabase.SaveAssets();
  return "Replaced the calibration line drawing with the saved seated 3D human and chair. Authored isolated camera/RenderTexture; rest/raised pose guide and live sensor preview use the existing HumanLegTracker component. Gameplay scene was not saved or changed.";
 }
}
