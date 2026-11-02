using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;
public static class AuthorModeGameplayPreview
{
    static TMP_Text style;
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var existing=parent.Find(name);
        var r=existing?existing.GetComponent<RectTransform>():new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent,false);r.gameObject.layer=5;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;
        r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;
    }
    static UnityEngine.UI.Image Image(RectTransform r,Color colour,Sprite sprite=null)
    {
        var i=r.GetComponent<UnityEngine.UI.Image>();if(!i)i=r.gameObject.AddComponent<UnityEngine.UI.Image>();
        i.color=colour;i.sprite=sprite;i.raycastTarget=false;return i;
    }
    static TMP_Text Text(Transform p,string name,float x,float y,float w,float h,string value,float size,Color colour)
    {
        var r=Rect(p,name,x,y,w,h);var t=r.GetComponent<TextMeshProUGUI>();if(!t)t=r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=style.font;t.fontSharedMaterial=style.fontSharedMaterial;t.fontSize=size;t.color=colour;t.text=value;
        t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
    }
    static void Bind(SerializedObject so,string name,UnityEngine.Object value)=>so.FindProperty(name).objectReferenceValue=value;
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="ModeSelection"||scene.isDirty)throw new Exception("Open saved ModeSelection first.");
        var canvas=GameObject.Find("/Canvas").transform;var controller=UnityEngine.Object.FindAnyObjectByType<SelectionPageController>();
        var therapy=canvas.Find("TherapyCard");var free=canvas.Find("FreeCard");
        if(!therapy||!free)throw new Exception("Existing mode cards missing.");
        style=canvas.Find("Title").GetComponent<TMP_Text>();
        var gameScene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity",OpenSceneMode.Additive);
        var playfield=gameScene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameplayPlayfield>(true)).First();
        var circle=((UnityEngine.UI.Image)new SerializedObject(playfield).FindProperty("noteImages").GetArrayElementAtIndex(0).objectReferenceValue).sprite;
        EditorSceneManager.CloseScene(gameScene,true);
        var manager=controller.GetComponent<ModeSelectionPreview>();if(!manager)manager=controller.gameObject.AddComponent<ModeSelectionPreview>();
        var managerConfig=new SerializedObject(manager);var cards=managerConfig.FindProperty("cards");cards.arraySize=2;
        var transforms=new[]{therapy,free};for(int i=0;i<2;i++){var group=transforms[i].GetComponent<CanvasGroup>();if(!group)group=transforms[i].gameObject.AddComponent<CanvasGroup>();cards.GetArrayElementAtIndex(i).objectReferenceValue=group;}
        var panel=Rect(canvas,"ModeGameplayPreview",425,-65,760,600);
        var background=Image(panel,new Color(.035f,.045f,.105f,1),therapy.GetComponent<UnityEngine.UI.Image>().sprite);background.type=UnityEngine.UI.Image.Type.Sliced;background.raycastTarget=true;
        var panelGroup=panel.GetComponent<CanvasGroup>();if(!panelGroup)panelGroup=panel.gameObject.AddComponent<CanvasGroup>();
        var demo=panel.GetComponent<ModeGameplayDemo>();if(!demo)demo=panel.gameObject.AddComponent<ModeGameplayDemo>();
        var accent=Image(Rect(panel,"Accent",0,282,680,3),new Color(.68f,.32f,1));
        var title=Text(panel,"PreviewTitle",0,244,680,48,"GAMEPLAY PREVIEW",31,Color.white);
        Text(panel,"AutoDemo",0,198,680,30,"AUTO DEMO  ·  NO INPUT NEEDED",20,new Color(.55f,.7f,.86f));
        var plot=Rect(panel,"Playfield",0,15,680,300);Image(plot,new Color(.005f,.025f,.055f,1));
        if(!plot.GetComponent<UnityEngine.UI.RectMask2D>())plot.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        var star=AssetDatabase.LoadAllAssetsAtPath("Assets/Assets/Space_Exploration_GUI_Kit/Other_Images/Large/sparkle-large.png").OfType<Sprite>().First();
        for(int i=0;i<10;i++)Image(Rect(plot,"Star"+i,-205+(i*61)%520,-120+(i*47)%230,5+i%3*2,5+i%3*2),new Color(.3f,.5f,.8f,.35f),star);
        Color[] colours={new Color(.3f,1,.45f),new Color(1,.75f,.15f),new Color(1,.3f,.5f)};
        for(int i=0;i<3;i++)
        {
            var tint=colours[i];tint.a=.4f;Image(Rect(plot,"Guide"+i,40,(i-1)*100,580,2),tint);
            Text(plot,"HeightLabel"+i,-290,(i-1)*100+12,78,25,new[]{"LOW","MID","HIGH"}[i],17,colours[i]);
        }
        Image(Rect(plot,"JudgementLine",-235,0,2,250),new Color(.7f,.3f,1));
        Text(plot,"JudgementLabel",-235,-134,180,20,"JUDGEMENT LINE",12,new Color(.7f,.45f,1));
        var score=Text(plot,"Score",95,130,380,25,"DEMO SCORE  0",18,new Color(.7f,.79f,.9f));
        var feedback=Text(plot,"Feedback",35,40,430,48,"",26,new Color(1,.85f,.35f));
        var heads=new RectTransform[6];var holds=new RectTransform[6];var images=new UnityEngine.UI.Image[6];
        for(int i=0;i<heads.Length;i++)
        {
            heads[i]=Rect(plot,"Note"+i,300,0,20,20);images[i]=Image(heads[i],colours[0],circle);
            holds[i]=Rect(heads[i],"HoldBody",12,0,200,7);holds[i].pivot=new Vector2(0,.5f);Image(holds[i],new Color(.3f,1,.45f,.45f));
        }
        var marker=Rect(plot,"HeightTracker",-235,0,26,26);Image(marker,Color.white,circle);
        var explanation=Text(panel,"Explanation",0,-180,680,80,"Preview the note heights before continuing.",25,new Color(.77f,.84f,.95f));
        var close=Rect(panel,"BackToModes",0,-258,360,54);var closeImage=Image(close,new Color(.14f,.12f,.3f));closeImage.raycastTarget=true;
        var outline=close.GetComponent<UnityEngine.UI.Outline>();if(!outline)outline=close.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(.55f,.4f,.95f,.65f);outline.effectDistance=new Vector2(1,-1);
        var button=close.GetComponent<UnityEngine.UI.Button>();if(!button)button=close.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=closeImage;
        button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,manager.Close);
        Text(close,"Label",0,0,340,48,"BACK TO MODES",23,Color.white);
        if(!close.GetComponent<UIHoverFeedback>())close.gameObject.AddComponent<UIHoverFeedback>();
        var click=close.GetComponent<UIClickSound>();if(!click)click=close.gameObject.AddComponent<UIClickSound>();var clickConfig=new SerializedObject(click);Bind(clickConfig,"screenAudio",UnityEngine.Object.FindAnyObjectByType<UIScreenAudio>());clickConfig.ApplyModifiedPropertiesWithoutUndo();
        var config=new SerializedObject(demo);Bind(config,"title",title);Bind(config,"explanation",explanation);Bind(config,"feedback",feedback);Bind(config,"score",score);Bind(config,"accent",accent);Bind(config,"marker",marker);
        foreach(var name in new[]{"heads","holdBodies","noteImages"}){var array=config.FindProperty(name);array.arraySize=6;for(int i=0;i<6;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=name=="heads"?heads[i]:name=="holdBodies"?holds[i]:(UnityEngine.Object)images[i];}
        config.ApplyModifiedPropertiesWithoutUndo();Bind(managerConfig,"preview",panelGroup);Bind(managerConfig,"demo",demo);managerConfig.ApplyModifiedPropertiesWithoutUndo();
        var pageConfig=new SerializedObject(controller);Bind(pageConfig,"modePreview",manager);pageConfig.ApplyModifiedPropertiesWithoutUndo();
        canvas.Find("Subtitle").GetComponent<TMP_Text>().text="Select a mode to preview how it plays.";
        panelGroup.alpha=0;panelGroup.interactable=panelGroup.blocksRaycasts=false;panel.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=controller.gameObject;
        return "Saved two-card preview swap, six authored demo notes with a hold, fixed/varied height demos, and Back to modes control.";
    }
}
