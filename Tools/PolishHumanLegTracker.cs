using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class PolishHumanLegTracker
{
    static Transform Bone(Transform root,string name)=>root.GetComponentsInChildren<Transform>().Single(t=>t.name==name);
    static void Aim(Transform bone,Transform child,Vector3 direction)=>bone.rotation=Quaternion.FromToRotation(child.position-bone.position,direction)*bone.rotation;
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="Gameplay"||scene.isDirty)throw new Exception("Open saved Gameplay first.");
        var body=GameObject.Find("HumanLegTrackerStage/SeatedHuman").transform;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M White.prefab");
        foreach(string side in new[]{"L","R"})
        {
            var fore=Bone(body,"B-forearm."+side);var hand=Bone(body,"B-hand."+side);
            Aim(fore,hand,new Vector3(0,-.65f,.8f));Aim(hand,Bone(body,"B-middleFinger01."+side),new Vector3(0,-.2f,1));
            Bone(body,"B-foot."+side).rotation=Bone(source.transform,"B-foot."+side).rotation;
        }
        GameObject.Find("/Canvas/HumanLegTracker/HumanView").GetComponent<UnityEngine.UI.RawImage>().uvRect=new Rect(1,0,-1,1);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Side view faces the notes; hands rest near the lap.";
    }
}
