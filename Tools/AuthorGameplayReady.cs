using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;
public static class AuthorGameplayReady
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Save scene first.");
        if(scene.name!="Gameplay")scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
        var canvas=GameObject.Find("/Canvas").transform;var hud=UnityEngine.Object.FindAnyObjectByType<GameplayHud>();
        var existing=canvas.Find("ReadyOverlay");var root=existing?existing.gameObject:new GameObject("ReadyOverlay",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.Image),typeof(CanvasGroup));
        var rect=root.GetComponent<RectTransform>();rect.SetParent(canvas,false);rect.SetAsLastSibling();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;root.layer=5;
        var image=root.GetComponent<UnityEngine.UI.Image>();image.color=new Color(0,0,0,.8f);image.raycastTarget=true;
        var group=root.GetComponent<CanvasGroup>();group.alpha=1;group.blocksRaycasts=true;
        var child=rect.Find("ReadyWord");var word=child?child.gameObject:new GameObject("ReadyWord",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));
        var r=word.GetComponent<RectTransform>();r.SetParent(rect,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(1100,220);word.layer=5;
        var label=word.GetComponent<TextMeshProUGUI>();var style=(TMP_Text)new SerializedObject(hud).FindProperty("scoreLabel").objectReferenceValue;
        label.font=style.font;label.fontSharedMaterial=style.fontSharedMaterial;label.fontSize=140;label.text="READY";label.color=Color.white;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
        var so=new SerializedObject(hud);so.FindProperty("readyOverlay").objectReferenceValue=group;so.FindProperty("readyDuration").floatValue=3f;so.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(true);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=root;
        return "Saved full-screen 80% black READY overlay. Holds for 3 seconds before gameplay clock and audio begin; no countdown.";
    }
}
