using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using RhythmRehab;

public static class WireSelectionUI
{
    static T Get<T>(string path) where T:Component
    {
        var go=GameObject.Find(path);
        if(!go)throw new Exception("Missing object: "+path);
        var c=go.GetComponent<T>();if(!c)throw new Exception("Missing "+typeof(T).Name+": "+path);return c;
    }
    static void Assign(SerializedObject so,string field,UnityEngine.Object value)=>so.FindProperty(field).objectReferenceValue=value;
    static void Click(string path,UnityAction action)
    {
        var b=Get<Button>(path);b.targetGraphic=b.GetComponent<Image>();
        for(int i=0;i<b.onClick.GetPersistentEventCount();i++)if(b.onClick.GetPersistentTarget(i)==action.Target as UnityEngine.Object && b.onClick.GetPersistentMethodName(i)==action.Method.Name)return;
        UnityEventTools.AddPersistentListener(b.onClick,action);EditorUtility.SetDirty(b);
    }
    static SongCatalog Catalog()
    {
        const string path="Assets/Data/PlaceholderSongs.asset";
        var catalog=AssetDatabase.LoadAssetAtPath<SongCatalog>(path);
        if(catalog!=null)return catalog;
        if(!AssetDatabase.IsValidFolder("Assets/Data"))AssetDatabase.CreateFolder("Assets","Data");
        catalog=ScriptableObject.CreateInstance<SongCatalog>();
        string[] names={"Neon Steps","Morning Motion","Steady Rise","Skyline Pulse","Gentle Waves","Rhythm Circuit","Moonlight Walk","Bright Horizon"};
        int[] bpms={120,100,110,128,90,140,108,132};
        Color[] colors={new Color(.30f,.12f,.60f),new Color(.08f,.42f,.50f),new Color(.32f,.30f,.08f),new Color(.12f,.25f,.65f),new Color(.06f,.38f,.30f),new Color(.58f,.12f,.30f),new Color(.22f,.16f,.48f),new Color(.50f,.28f,.08f)};
        catalog.songs=new SongEntry[names.Length];
        for(int i=0;i<names.Length;i++)catalog.songs[i]=new SongEntry{id="demo-"+i,title=names[i],artist="RhythmRehab Demo",bpm=bpms[i],coverColor=colors[i]};
        AssetDatabase.CreateAsset(catalog,path);AssetDatabase.SaveAssets();return catalog;
    }
    public static string Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var catalog=Catalog();
        if(scene.name=="SongSelection")
        {
            var controller=Get<SongSelectionController>("/Controllers");var so=new SerializedObject(controller);
            Assign(so,"catalog",catalog);
            var scroll=Get<ScrollRect>("/Canvas/SongScroll");
            scroll.viewport=Get<RectTransform>("/Canvas/SongScroll/Viewport");scroll.content=Get<RectTransform>("/Canvas/SongScroll/Viewport/Content");
            scroll.horizontal=true;scroll.vertical=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=70;EditorUtility.SetDirty(scroll);
            Assign(so,"songScroll",scroll);
            var input=Get<TMP_InputField>("/Canvas/Search");input.textViewport=Get<RectTransform>("/Canvas/Search/TextArea");
            input.textComponent=Get<TMP_Text>("/Canvas/Search/TextArea/Text");input.placeholder=Get<TMP_Text>("/Canvas/Search/TextArea/Placeholder");input.targetGraphic=input.GetComponent<Image>();input.lineType=TMP_InputField.LineType.SingleLine;
            UnityEventTools.AddPersistentListener(input.onValueChanged,controller.Filter);EditorUtility.SetDirty(input);
            Assign(so,"search",input);Assign(so,"filterLabel",Get<TMP_Text>("/Canvas/FilterButton/Text (TMP)"));
            Assign(so,"selectionLabel",Get<TMP_Text>("/Canvas/SelectionLabel"));Assign(so,"browseLabel",Get<TMP_Text>("/Canvas/BrowseLabel"));
            Assign(so,"emptyMessage",GameObject.Find("/Canvas/SongScroll/EmptyMessage"));Assign(so,"continueButton",Get<Button>("/Canvas/ContinueButton"));
            var cards=so.FindProperty("cards");cards.arraySize=8;
            for(int i=0;i<8;i++)
            {
                var path="/Canvas/SongScroll/Viewport/Content/Song"+i;var card=Get<SongCardView>(path);var cs=new SerializedObject(card);
                Assign(cs,"owner",controller);cs.FindProperty("songIndex").intValue=i;
                Assign(cs,"title",Get<TMP_Text>(path+"/SongTitle"));Assign(cs,"artist",Get<TMP_Text>(path+"/Artist"));Assign(cs,"tempo",Get<TMP_Text>(path+"/Tempo"));
                Assign(cs,"cover",Get<Image>(path+"/Cover"));Assign(cs,"selectionOutline",Get<Outline>(path));Assign(cs,"selectedBadge",GameObject.Find(path+"/SelectedBadge"));
                cs.ApplyModifiedProperties();card.Bind(catalog.songs[i]);card.SetSelected(i==0);EditorUtility.SetDirty(card);
                cards.GetArrayElementAtIndex(i).objectReferenceValue=card;Click(path,card.Select);
            }
            so.ApplyModifiedProperties();
            Click("/Canvas/BackButton",controller.Home);Click("/Canvas/HomeButton",controller.Home);Click("/Canvas/PreviousButton",controller.Previous);Click("/Canvas/NextButton",controller.Next);Click("/Canvas/FilterButton",controller.CycleFilter);Click("/Canvas/ContinueButton",controller.Continue);
            GameObject.Find("/Canvas/SongScroll/EmptyMessage").SetActive(false);
            Canvas.ForceUpdateCanvases();scroll.horizontalNormalizedPosition=0;
        }
        else if(scene.name=="ModeSelection"||scene.name=="ExerciseSelection"||scene.name=="DifficultySelection")
        {
            var controller=Get<SelectionPageController>("/Controllers");var so=new SerializedObject(controller);
            Assign(so,"catalog",catalog);Assign(so,"songContext",Get<TMP_Text>("/Canvas/SongContext"));
            bool mode=scene.name=="ModeSelection";
            so.FindProperty("page").intValue=mode?0:scene.name=="ExerciseSelection"?1:2;
            if(!mode)
            {
                Assign(so,"selectedLabel",Get<TMP_Text>("/Canvas/SelectedLabel"));
                var outlines=so.FindProperty("choiceOutlines");outlines.arraySize=3;
                var badges=so.FindProperty("choiceBadges");badges.arraySize=3;
                for(int i=0;i<3;i++)
                {
                    string path="/Canvas/Choice"+i;outlines.GetArrayElementAtIndex(i).objectReferenceValue=Get<Outline>(path);
                    badges.GetArrayElementAtIndex(i).objectReferenceValue=GameObject.Find(path+"/SelectedBadge");
                    var b=Get<Button>(path);b.targetGraphic=b.GetComponent<Image>();UnityEventTools.AddIntPersistentListener(b.onClick,controller.Choose,i);EditorUtility.SetDirty(b);
                    Get<Outline>(path).enabled=i==0;GameObject.Find(path+"/SelectedBadge").SetActive(i==0);
                }
            }
            so.ApplyModifiedProperties();Click("/Canvas/BackButton",controller.Back);Click("/Canvas/HomeButton",controller.Home);
            if(mode){Click("/Canvas/TherapyCard",controller.Therapy);Click("/Canvas/FreeCard",controller.Free);}
            else Click("/Canvas/ContinueButton",controller.BeginGame);
        }
        else if(scene.name=="Gameplay")
        {
            var so=GameplayEditorAccess.Owner(Get<PrototypeGameplay>("/Controllers"),"songLabel");Assign(so,"songLabel",Get<TMP_Text>("/Canvas/PrototypeBadge"));Assign(so,"modeLabel",Get<TMP_Text>("/Canvas/Sidebar/Mode"));so.ApplyModifiedProperties();
        }
        else if(scene.name=="Results")
        {
            var so=new SerializedObject(Get<ResultsPresenter>("/Controllers"));Assign(so,"sessionContext",Get<TMP_Text>("/Canvas/Subtitle"));so.ApplyModifiedProperties();
            var nav=Get<SceneNavigation>("/Controllers");var button=Get<Button>("/Canvas/ReplayButton");
            for(int i=button.onClick.GetPersistentEventCount()-1;i>=0;i--)if(button.onClick.GetPersistentTarget(i)==nav && button.onClick.GetPersistentMethodName(i)=="Play")UnityEventTools.RemovePersistentListener(button.onClick,i);
            Click("/Canvas/ReplayButton",nav.Replay);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return scene.name+" selection references and callbacks saved";
    }
}
