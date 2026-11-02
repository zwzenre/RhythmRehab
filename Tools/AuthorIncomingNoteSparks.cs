using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class AuthorIncomingNoteSparks
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="Gameplay"||scene.isDirty)throw new Exception("Open saved Gameplay scene first.");
        var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();
        var field=game.GetComponent<GameplayPlayfield>();
        var notes=new SerializedObject(field).FindProperty("noteViews");
        var sprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Assets/Space_Exploration_GUI_Kit/Other_Images/Large/sparkle-large.png").OfType<Sprite>().FirstOrDefault();
        if(!sprite)throw new Exception("GUI kit sparkle sprite not imported.");
        for(int i=0;i<notes.arraySize;i++)
        {
            var head=(RectTransform)notes.GetArrayElementAtIndex(i).objectReferenceValue;
            var child=head.Find("IncomingSparks");
            var rect=child?child.GetComponent<RectTransform>():new GameObject("IncomingSparks",typeof(RectTransform),typeof(CanvasRenderer),typeof(IncomingNoteSparks)).GetComponent<RectTransform>();
            rect.SetParent(head,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(520,120);rect.SetAsFirstSibling();
            var graphic=rect.GetComponent<IncomingNoteSparks>();graphic.raycastTarget=false;
            var so=new SerializedObject(graphic);
            so.FindProperty("gameplay").objectReferenceValue=game;so.FindProperty("playfield").objectReferenceValue=field;
            so.FindProperty("noteImage").objectReferenceValue=head.GetComponent<UnityEngine.UI.Image>();so.FindProperty("sparkle").objectReferenceValue=sprite;
            so.FindProperty("sparkCount").intValue=14;so.FindProperty("sparkSize").floatValue=28;
            so.FindProperty("trailLength").floatValue=195;so.FindProperty("spread").floatValue=26;so.FindProperty("opacity").floatValue=.85f;
            so.FindProperty("phase").floatValue=i*.137f;so.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Saved GUI-kit spark trails on "+notes.arraySize+" authored note slots.";
    }
}
