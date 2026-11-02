using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;
public static class AuthorHoldNotes
{
    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var old=parent.Find(name);var rect=old?old.GetComponent<RectTransform>():new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
    static UnityEngine.UI.Image Image(RectTransform rect)
    {
        var image=rect.GetComponent<UnityEngine.UI.Image>();if(!image)image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.raycastTarget=false;return image;
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Save current scene first.");
        if(scene.name!="Gameplay")scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
        var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();var config=new SerializedObject(game);
        var visuals=GameplayEditorAccess.Owner(game,"noteViews");var notes=visuals.FindProperty("noteViews");var views=visuals.FindProperty("holdViews");views.arraySize=notes.arraySize;
        var font=GameObject.Find("/Canvas/BottomBar/HoldingTip").GetComponent<TMP_Text>().font;
        for(int i=0;i<notes.arraySize;i++)
        {
            var note=(RectTransform)notes.GetArrayElementAtIndex(i).objectReferenceValue;
            var body=Rect(note,"HoldBody",new Vector2(18,0),new Vector2(300,18));body.pivot=new Vector2(0,.5f);var bodyImage=Image(body);
            bodyImage.color=new Color(.3f,1,.45f,.3f);
            var shine=Rect(body,"Shine",Vector2.zero,new Vector2(-4,3));shine.anchorMin=new Vector2(0,.5f);shine.anchorMax=new Vector2(1,.5f);var shineImage=Image(shine);
            var tail=Rect(body,"Tail",Vector2.zero,Vector2.one*22);tail.anchorMin=tail.anchorMax=new Vector2(1,.5f);var tailImage=Image(tail);tailImage.sprite=note.GetComponent<UnityEngine.UI.Image>().sprite;
            var textRect=Rect(note,"HoldCaption",new Vector2(34,65),new Vector2(185,32));textRect.pivot=new Vector2(0,.5f);
            var text=textRect.GetComponent<TextMeshProUGUI>();if(!text)text=textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font=font;text.fontSize=18;text.text="HOLD";text.color=new Color(.82f,.9f,1,.9f);text.alignment=TextAlignmentOptions.MidlineLeft;text.raycastTarget=false;
            var view=note.GetComponent<HoldNoteView>();if(!view)view=note.gameObject.AddComponent<HoldNoteView>();
            var v=new SerializedObject(view);v.FindProperty("body").objectReferenceValue=bodyImage;v.FindProperty("shine").objectReferenceValue=shineImage;
            v.FindProperty("tail").objectReferenceValue=tailImage;v.FindProperty("caption").objectReferenceValue=text;v.ApplyModifiedPropertiesWithoutUndo();
            view.Hide();views.GetArrayElementAtIndex(i).objectReferenceValue=view;
        }
        var chart=config.FindProperty("chart");
        for(int i=0;i<chart.arraySize;i++)
        {
            var item=chart.GetArrayElementAtIndex(i);item.FindPropertyRelative("holdBeats").floatValue=i%6==3?2:0;
            if(i>0)
            {
                var before=chart.GetArrayElementAtIndex(i-1);float earliest=before.FindPropertyRelative("beat").floatValue+before.FindPropertyRelative("holdBeats").floatValue+1;
                var beat=item.FindPropertyRelative("beat");beat.floatValue=Mathf.Max(beat.floatValue,earliest);
            }
        }
        config.ApplyModifiedPropertiesWithoutUndo();visuals.ApplyModifiedPropertiesWithoutUndo();
        GameObject.Find("/Canvas/BottomBar/HoldingTip").GetComponent<TMP_Text>().text="Tap: SPACE on the beat. Long notes: keep SPACE held and maintain height until the tail ends.";
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Saved hold bodies, tail caps and progress captions on all "+notes.arraySize+" note slots. Demo chart includes two-beat holds with recovery gaps.";
    }
}
