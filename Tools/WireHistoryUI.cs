using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.Events;
using TMPro;
using RhythmRehab;

public static class WireHistoryUI
{
    static T Get<T>(string path) where T:Component => GameObject.Find(path).GetComponent<T>();
    static void Click(string path,UnityAction action)
    {
        var b=Get<Button>(path);b.targetGraphic=b.GetComponent<Image>();
        for(int i=0;i<b.onClick.GetPersistentEventCount();i++)
            if(b.onClick.GetPersistentMethodName(i)==action.Method.Name&&b.onClick.GetPersistentTarget(i)==action.Target as UnityEngine.Object)return;
        UnityEventTools.AddPersistentListener(b.onClick,action);EditorUtility.SetDirty(b);
    }
    public static string Main()
    {
        var p=Get<PerformanceHistoryPresenter>("/Controllers");var nav=Get<SceneNavigation>("/Controllers");var so=new SerializedObject(p);
        Action<string,UnityEngine.Object> assign=(f,o)=>so.FindProperty(f).objectReferenceValue=o;
        string g="/Canvas/GraphPanel",l="/Canvas/SessionPanel",plot=g+"/Plot";
        assign("overview",Get<TMP_Text>("/Canvas/Overview"));assign("graphTitle",Get<TMP_Text>(g+"/GraphTitle"));
        assign("graphMessage",Get<TMP_Text>(g+"/GraphMessage"));assign("graphRange",Get<TMP_Text>(g+"/GraphRange"));
        assign("listTitle",Get<TMP_Text>(l+"/ListTitle"));assign("pageLabel",Get<TMP_Text>("/Canvas/PageLabel"));
        assign("previousButton",Get<Button>("/Canvas/PreviousButton"));assign("nextButton",Get<Button>("/Canvas/NextButton"));
        assign("viewAllLabel",Get<TMP_Text>(l+"/ViewAllButton/Text (TMP)"));
        var scroll=Get<ScrollRect>(l+"/SessionScroll");scroll.viewport=Get<RectTransform>(l+"/SessionScroll/Viewport");scroll.content=Get<RectTransform>(l+"/SessionScroll/Viewport/Content");
        scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=55;EditorUtility.SetDirty(scroll);assign("sessionScroll",scroll);
        Action<string,int,Func<int,UnityEngine.Object>> array=(name,count,get)=>{var a=so.FindProperty(name);a.arraySize=count;for(int i=0;i<count;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=get(i);};
        array("rows",10,i=>Get<TMP_Text>(l+"/SessionScroll/Viewport/Content/Row"+i+"/Text"));
        array("points",8,i=>Get<RectTransform>(plot+"/Point"+i));array("segments",7,i=>Get<RectTransform>(plot+"/Segment"+i));
        array("valueLabels",8,i=>Get<TMP_Text>(plot+"/Value"+i));array("dateLabels",8,i=>Get<TMP_Text>(plot+"/Date"+i));
        array("axisLabels",5,i=>Get<TMP_Text>(plot+"/Axis"+i));array("tabImages",4,i=>Get<Image>(g+"/Metric"+i));
        for(int i=0;i<4;i++){var b=Get<Button>(g+"/Metric"+i);b.targetGraphic=b.GetComponent<Image>();UnityEventTools.AddIntPersistentListener(b.onClick,p.ChooseMetric,i);EditorUtility.SetDirty(b);}
        so.ApplyModifiedProperties();
        Click("/Canvas/HomeButton",nav.Home);Click("/Canvas/BackButton",nav.Home);Click(l+"/ViewAllButton",p.ToggleAll);
        Click("/Canvas/PreviousButton",p.Previous);Click("/Canvas/NextButton",p.Next);
        var sprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Assets/Space_Exploration_GUI_Kit/Picto_Icons/White/full-moon-128.png").OfType<Sprite>().OrderByDescending(s=>s.rect.width*s.rect.height).First();
        for(int i=0;i<8;i++){var image=Get<Image>(plot+"/Point"+i);image.sprite=sprite;EditorUtility.SetDirty(image);GameObject.Find(plot+"/Point"+i).SetActive(false);}
        for(int i=0;i<7;i++)GameObject.Find(plot+"/Segment"+i).SetActive(false);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Click("/MenuCanvas/MenuButtons/ProfileButton",Get<SceneNavigation>("/PrototypeNavigation").Profile);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "History references, graph tabs, scrolling, pagination and main-menu PROFILE wired.";
    }
}
