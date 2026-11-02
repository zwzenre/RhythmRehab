using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
using TMPro;

public static class AuthorHumanLegTracker
{
    const string Folder="Assets/UI/LegTracker";
    const int Layer=30;
    static Transform Bone(Transform model,string name)=>model.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
    static void Aim(Transform bone,Transform child,Vector3 direction)
    {
        bone.rotation=Quaternion.FromToRotation(child.position-bone.position,direction)*bone.rotation;
    }
    static Material Material(string name,Color colour)
    {
        string path=Folder+"/"+name+".mat";
        var result=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!result){result=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));AssetDatabase.CreateAsset(result,path);}
        result.SetColor("_BaseColor",colour);result.SetFloat("_Smoothness",.12f);
        EditorUtility.SetDirty(result);return result;
    }
    static void Cube(Transform parent,string name,Vector3 pos,Vector3 size,Material material)
    {
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name=name;cube.transform.SetParent(parent,false);
        cube.transform.localPosition=pos;cube.transform.localScale=size;cube.layer=Layer;
        UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        var r=cube.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    static RectTransform Rect(Transform parent,string name,Vector2 pos,Vector2 size)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=pos;rect.sizeDelta=size;return rect;
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene first.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
        if(GameObject.Find("HumanLegTrackerStage"))throw new Exception("Human tracker already exists; edit it in place.");
        var gameplay=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();
        if(!gameplay)throw new Exception("Gameplay controller missing.");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/UI","LegTracker");
        var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layer=tags.FindProperty("layers").GetArrayElementAtIndex(Layer);
        if(!string.IsNullOrEmpty(layer.stringValue)&&layer.stringValue!="LegTracker")throw new Exception("Layer 30 already in use.");
        layer.stringValue="LegTracker";tags.ApplyModifiedPropertiesWithoutUndo();
        var stage=new GameObject("HumanLegTrackerStage");stage.transform.position=new Vector3(100,0,0);
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M White.prefab");
        if(!asset)throw new Exception("Imported human prefab missing.");
        var body=(GameObject)PrefabUtility.InstantiatePrefab(asset,stage.transform);
        PrefabUtility.UnpackPrefabInstance(body,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        body.name="SeatedHuman";body.transform.localPosition=Vector3.zero;
        foreach(var t in body.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
        foreach(var animator in body.GetComponentsInChildren<Animator>(true))animator.enabled=false;
        var hips=Bone(body.transform,"B-hips");hips.localPosition+=new Vector3(0,-.38f,0);
        foreach(string side in new[]{"L","R"})
        {
            var thigh=Bone(body.transform,"B-thigh."+side);var shin=Bone(body.transform,"B-shin."+side);
            var foot=Bone(body.transform,"B-foot."+side);var toe=Bone(body.transform,"B-toe."+side);
            Aim(thigh,shin,Vector3.forward);Aim(shin,foot,Vector3.down);
            // The toe joint sits below the ankle in this rig; preserve the bind foot orientation.
            foot.rotation=Bone(asset.transform,"B-foot."+side).rotation;
            var upper=Bone(body.transform,"B-upperArm."+side);var fore=Bone(body.transform,"B-forearm."+side);var hand=Bone(body.transform,"B-hand."+side);
            Aim(upper,fore,new Vector3(0,-1,.2f));Aim(fore,hand,new Vector3(0,-.65f,.8f));
            Aim(hand,Bone(body.transform,"B-middleFinger01."+side),new Vector3(0,-.2f,1));
        }
        var skin=Material("TrackerHuman",new Color(.56f,.76f,.88f));
        foreach(var renderer in body.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderer.sharedMaterial=skin;renderer.updateWhenOffscreen=true;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var chair=new GameObject("Chair");chair.transform.SetParent(stage.transform,false);
        var chairMat=Material("TrackerChair",new Color(.15f,.17f,.3f));
        Cube(chair.transform,"Seat",new Vector3(0,.45f,.09f),new Vector3(.48f,.07f,.48f),chairMat);
        Cube(chair.transform,"Back",new Vector3(0,.78f,-.17f),new Vector3(.48f,.65f,.06f),chairMat);
        foreach(float x in new[]{-.19f,.19f})foreach(float z in new[]{-.1f,.28f})Cube(chair.transform,"ChairLeg",new Vector3(x,.23f,z),new Vector3(.045f,.44f,.045f),chairMat);
        var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>(Folder+"/HumanLegView.renderTexture");
        if(!rt){rt=new RenderTexture(512,768,24,RenderTextureFormat.ARGB32);rt.name="HumanLegView";AssetDatabase.CreateAsset(rt,Folder+"/HumanLegView.renderTexture");}
        var cameraObject=new GameObject("LegTrackerCamera",typeof(Camera));cameraObject.transform.SetParent(stage.transform,false);
        cameraObject.transform.localPosition=new Vector3(-4,.76f,.38f);cameraObject.transform.localRotation=Quaternion.LookRotation(Vector3.right,Vector3.up);
        var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.95f;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<Layer;
        camera.targetTexture=rt;camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.allowHDR=false;camera.allowMSAA=false;camera.depth=-10;
        foreach(var existing in UnityEngine.Object.FindObjectsByType<Camera>())if(existing!=camera)existing.cullingMask&=~(1<<Layer);
        var lightObject=new GameObject("TrackerKeyLight",typeof(Light));lightObject.transform.SetParent(stage.transform,false);
        lightObject.transform.localPosition=new Vector3(-2,2,1);lightObject.transform.LookAt(stage.transform.position+Vector3.up*.7f);
        var light=lightObject.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.cullingMask=1<<Layer;light.shadows=LightShadows.None;
        var tracker=stage.AddComponent<HumanLegTracker>();var trackerConfig=new SerializedObject(tracker);
        trackerConfig.FindProperty("gameplay").objectReferenceValue=gameplay;trackerConfig.FindProperty("model").objectReferenceValue=body.transform;
        trackerConfig.FindProperty("lowerLeg").objectReferenceValue=Bone(body.transform,"B-shin.L");trackerConfig.ApplyModifiedPropertiesWithoutUndo();
        var canvas=GameObject.Find("/Canvas").transform;var field=GameObject.Find("/Canvas/Playfield").transform;
        var ui=Rect(canvas,"HumanLegTracker",new Vector2(-780,0),new Vector2(340,700));
        ui.SetSiblingIndex(field.GetSiblingIndex()+1);
        var viewRect=Rect(ui,"HumanView",new Vector2(0,0),new Vector2(340,510));
        var view=viewRect.gameObject.AddComponent<UnityEngine.UI.RawImage>();view.texture=rt;view.raycastTarget=false;
        view.uvRect=new Rect(1,0,-1,1);
        var title=Rect(ui,"Title",new Vector2(0,320),new Vector2(320,40)).gameObject.AddComponent<TextMeshProUGUI>();
        title.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Assets/Space_Exploration_GUI_Kit/Fonts/Asap-Bold SDF.asset");title.text="YOUR LEG MOVEMENT";
        title.fontSize=21;title.alignment=TextAlignmentOptions.Center;title.color=new Color(.65f,.75f,.95f);title.raycastTarget=false;
        var config=GameplayEditorAccess.Owner(gameplay,"judgementX");config.FindProperty("judgementX").floatValue=-550;config.ApplyModifiedPropertiesWithoutUndo();
        field.Find("JudgementLine").GetComponent<RectTransform>().anchoredPosition=new Vector2(-550,0);
        field.Find("LineLabel").GetComponent<RectTransform>().anchoredPosition=new Vector2(-550,-375);
        field.Find("PlayerMarker").GetComponent<RectTransform>().anchoredPosition=new Vector2(-550,-280);
        for(int i=0;i<7;i++)field.Find("Grid"+i).GetComponent<RectTransform>().anchoredPosition=new Vector2(Mathf.Lerp(-550,900,i/6f),0);
        foreach(string name in new[]{"LowLane","MidLane","HighLane"})
        {
            var lineRect=field.Find(name).GetComponent<RectTransform>();lineRect.anchorMin=new Vector2(0,.5f);lineRect.anchorMax=new Vector2(1,.5f);
            lineRect.offsetMin=new Vector2(380,lineRect.offsetMin.y);lineRect.offsetMax=new Vector2(-16,lineRect.offsetMax.y);
        }
        foreach(string name in new[]{"LowLabel","MidLabel","HighLabel"})
        {
            var label=field.Find(name).GetComponent<RectTransform>();label.anchoredPosition=new Vector2(-465,label.anchoredPosition.y);
        }
        var height=field.Find("HeightValue").GetComponent<RectTransform>();height.SetParent(ui,false);height.anchorMin=height.anchorMax=new Vector2(.5f,.5f);
        height.anchoredPosition=new Vector2(0,-295);height.sizeDelta=new Vector2(300,75);
        var heightText=height.GetComponent<TMP_Text>();heightText.alignment=TextAlignmentOptions.Center;heightText.fontSize=23;
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Saved seated dummy, knee-driven tracker, isolated orthographic render camera and compact left layout. Judgement line x=-550; spawn remains900.";
    }
}
