using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;

public static class AuthorGameplayVisualLevels
{
    const string Kit="Assets/Assets/Space_Exploration_GUI_Kit/";
    const string Vfx="Assets/Assets/BoldVFXPackDemo/Content/Atlas/Sprites/";
    static GameplayVisualLevels controller;
    static readonly List<UnityEngine.UI.Graphic>[] reactives={new(),new(),new(),new()};
    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;
    }
    static UnityEngine.UI.RawImage Image(Transform parent,string name,string asset,Vector2 position,Vector2 size,Color colour)
    {
        var image=Rect(parent,name,position,size).gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.texture=asset==null?Texture2D.whiteTexture:AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
        if(!image.texture)throw new Exception("Texture missing: "+asset);
        image.color=colour;image.raycastTarget=false;return image;
    }
    static VisualLevelPath Path(Transform parent,string name,Vector2[] points,Color colour,float width=2,bool closed=false,bool fill=false,Vector2 position=default)
    {
        var path=Rect(parent,name,position,new Vector2(1920,1080)).gameObject.AddComponent<VisualLevelPath>();
        path.color=colour;path.raycastTarget=false;var so=new SerializedObject(path);
        var array=so.FindProperty("points");array.arraySize=points.Length;for(int i=0;i<points.Length;i++)array.GetArrayElementAtIndex(i).vector2Value=points[i];
        so.FindProperty("lineWidth").floatValue=width;so.FindProperty("closed").boolValue=closed;so.FindProperty("fillToBottom").boolValue=fill;
        so.ApplyModifiedPropertiesWithoutUndo();return path;
    }
    static void Motion(Component target,VisualLevelMotion.Motion motion,Vector2 amplitude,float speed,float phase=0)
    {
        var component=target.gameObject.AddComponent<VisualLevelMotion>();var so=new SerializedObject(component);
        so.FindProperty("controller").objectReferenceValue=controller;so.FindProperty("motion").enumValueIndex=(int)motion;
        so.FindProperty("amplitude").vector2Value=amplitude;so.FindProperty("speed").floatValue=speed;so.FindProperty("phase").floatValue=phase;so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Vector2[] Arc(float radius,float start,float end,int count=45)
    {
        var points=new Vector2[count];for(int i=0;i<count;i++){float angle=Mathf.Lerp(start,end,i/(float)(count-1))*Mathf.Deg2Rad;points[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;}return points;
    }
    static Vector2[] Hex(float radius)=>Arc(radius,0,300,6);
    static RectTransform Group(Transform parent,string name)
    {
        var r=Rect(parent,name,Vector2.zero,Vector2.zero);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        var mask=r.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();mask.padding=new Vector4(370,150,0,95);mask.softness=new Vector2Int(40,40);return r;
    }
    static void Stars(Transform root,int count,int theme,bool fireflies)
    {
        for(int i=0;i<count;i++)
        {
            var colour=fireflies?new Color(.35f,.9f,.8f,.28f):new Color(.7f,.45f,1,.34f);
            var image=Image(root,(fireflies?"Firefly":"Star")+(i+1),fireflies?Vfx+"FX_Circle_Soft.png":Kit+"Other_Images/Small/sparkle-small.png",
                new Vector2(-510+Mathf.Repeat(i*281.3f,1430),-335+Mathf.Repeat(i*173.1f,690)),Vector2.one*(fireflies?7:8+i%4),colour);
            Motion(image,VisualLevelMotion.Motion.Drift,new Vector2(0,fireflies?14:5),fireflies?7+i%4:12+i%6,i*.8f);
            if(i%3==0)reactives[theme].Add(image);
        }
    }
    static void Therapy(Transform root)
    {
        Image(root,"QuietBlueLight",Vfx+"FX_Circle_Soft.png",new Vector2(580,-80),new Vector2(1050,900),new Color(.1f,.4f,.6f,.08f));
        var halo=Path(root,"OpenStudioArc",Arc(370,-75,90),new Color(.32f,.58f,.8f,.1f),2,false,false,new Vector2(600,15));
        Motion(halo,VisualLevelMotion.Motion.Float,new Vector2(16,8),.12f);reactives[0].Add(halo);
        for(int i=0;i<3;i++)
        {
            var line=Path(root,"CalmContour"+i,new[]{new Vector2(-530,-320+i*18),new Vector2(-70,-310+i*18),new Vector2(420,-330+i*18),new Vector2(950,-315+i*18)},new Color(.2f,.55f,.7f,.1f),1);
            Motion(line,VisualLevelMotion.Motion.Float,new Vector2(15,5),.12f,i);reactives[0].Add(line);
        }
    }
    static void Easy(Transform root)
    {
        Image(root,"HorizonGlow",Vfx+"FX_Circle_Soft.png",new Vector2(580,130),new Vector2(1000,850),new Color(.1f,.65f,.75f,.1f));
        var moon=Image(root,"Moon",Vfx+"FX_Circle_Hard.png",new Vector2(650,255),Vector2.one*126,new Color(.42f,.78f,.82f,.17f));
        Motion(moon,VisualLevelMotion.Motion.Float,new Vector2(10,7),.12f);reactives[1].Add(moon);
        var distant=Path(root,"DistantHills",new[]{new Vector2(-1100,-180),new Vector2(-650,-110),new Vector2(-320,-150),new Vector2(0,-60),new Vector2(290,-115),new Vector2(620,-65),new Vector2(1100,-160)},new Color(.05f,.3f,.35f,.45f),1,false,true);
        Motion(distant,VisualLevelMotion.Motion.Float,new Vector2(14,4),.09f);
        var near=Path(root,"NearHills",new[]{new Vector2(-1100,-245),new Vector2(-600,-215),new Vector2(-150,-245),new Vector2(240,-170),new Vector2(650,-220),new Vector2(1100,-180)},new Color(.02f,.14f,.22f,.65f),1,false,true);
        Motion(near,VisualLevelMotion.Motion.Float,new Vector2(22,4),.13f,1.8f);
        for(int i=0;i<2;i++)
        {
            var cloud=Image(root,"SoftCloud"+i,Kit+"Other_Images/Large/cloud-"+(i+1)+"-large.png",new Vector2(i==0?-120:680,i==0?250:90),new Vector2(580,190),new Color(.3f,.6f,.7f,.11f));
            Motion(cloud,VisualLevelMotion.Motion.Float,new Vector2(70,10),.12f+i*.03f,i*1.7f);
        }
        for(int i=0;i<4;i++)
        {
            var water=Path(root,"WaterContour"+i,new[]{new Vector2(-540,-290-i*18),new Vector2(-130,-275-i*18),new Vector2(350,-295-i*18),new Vector2(950,-280-i*18)},new Color(.2f,.7f,.72f,.14f),1);
            Motion(water,VisualLevelMotion.Motion.Float,new Vector2(34,5),.22f,i);reactives[1].Add(water);
        }
        Stars(root,18,1,true);
    }
    static void Medium(Transform root)
    {
        var halo=Image(root,"PlanetHalo",Vfx+"FX_Circle_Soft.png",new Vector2(630,160),Vector2.one*790,new Color(.45f,.13f,.8f,.11f));reactives[2].Add(halo);
        var planet=Image(root,"RingedPlanet",Kit+"Icons/exoplanet-5-512.png",new Vector2(650,180),Vector2.one*230,new Color(.8f,.6f,1,.27f));
        Motion(planet,VisualLevelMotion.Motion.Float,new Vector2(24,16),.18f);
        for(int i=0;i<3;i++)
        {
            var orbit=Path(root,"OrbitArc"+i,Arc(180+i*61,20+i*30,285+i*10),new Color(.65f,.3f,1,.17f),2,false,false,new Vector2(650,170));
            Motion(orbit,VisualLevelMotion.Motion.Rotate,Vector2.zero,(i%2==0?1:-1)*(1.5f+i));reactives[2].Add(orbit);
        }
        Path(root,"UpperConstellation",new[]{new Vector2(-460,230),new Vector2(-260,330),new Vector2(-70,215),new Vector2(100,310),new Vector2(270,210)},new Color(.65f,.35f,1,.16f),1);
        Path(root,"LowerConstellation",new[]{new Vector2(0,-305),new Vector2(220,-230),new Vector2(430,-320),new Vector2(620,-210),new Vector2(900,-300)},new Color(.5f,.4f,1,.14f),1);
        for(int i=0;i<2;i++)
        {
            var trail=Image(root,"ShootingStar"+i,Kit+"Other_Images/Large/shooting-star-"+(i==0?"pink":"blue")+"-large.png",new Vector2(i==0?-80:510,i==0?300:-285),new Vector2(280,110),new Color(.9f,.65f,1,.24f));
            Motion(trail,VisualLevelMotion.Motion.Float,new Vector2(130,30),.33f,i*2);reactives[2].Add(trail);
        }
        Stars(root,24,2,false);
    }
    static void Hard(Transform root)
    {
        Image(root,"ReactorGlow",Vfx+"FX_Circle_Soft.png",new Vector2(650,130),Vector2.one*820,new Color(1,.24f,.04f,.09f));
        for(int i=0;i<3;i++)
        {
            var hex=Path(root,"ReactorHexagon"+i,Hex(155+i*65),new Color(1,.5f,.12f,.16f-i*.025f),i==0?3:2,true,false,new Vector2(640,150));
            Motion(hex,VisualLevelMotion.Motion.Rotate,Vector2.zero,(i%2==0?1:-1)*(2+i));reactives[3].Add(hex);
        }
        for(int i=0;i<3;i++)
        {
            float y=i==0?340:i==1?-330:100;
            var line=Path(root,"CircuitTrace"+i,new[]{new Vector2(-540,y),new Vector2(-190,y),new Vector2(-100,y-70),new Vector2(180,y-70),new Vector2(280,y),new Vector2(960,y)},new Color(1,.42f,.1f,.17f),2);
            reactives[3].Add(line);
        }
        for(int i=0;i<12;i++)
        {
            var hex=Path(root,"HoneycombCell"+i,Hex(45),new Color(1,.45f,.12f,.085f),1,true,false,new Vector2(380+(i%4)*80,-235-(i/4)*70+(i%2)*35));
            if(i%4==0)reactives[3].Add(hex);
        }
        for(int i=0;i<8;i++)
        {
            var chevron=Path(root,"EnergyChevron"+i,new[]{new Vector2(-14,-14),Vector2.zero,new Vector2(-14,14)},new Color(1,.65f,.18f,.25f),2,false,false,new Vector2(-450+i*180,i%2==0?325:-320));
            Motion(chevron,VisualLevelMotion.Motion.Drift,Vector2.zero,55+i*4,i);reactives[3].Add(chevron);
        }
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Save current scene first.");
        if(scene.name!="Gameplay")scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
        var playfield=GameObject.Find("/Canvas/Playfield").transform;
        if(playfield.Find("VisualLevels"))throw new Exception("Visual levels already exist; edit in place.");
        var root=Rect(playfield,"VisualLevels",Vector2.zero,Vector2.zero);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;root.SetAsFirstSibling();
        controller=root.gameObject.AddComponent<GameplayVisualLevels>();
        var groups=new[]{Group(root,"Therapy_QuietStudio"),Group(root,"Easy_CalmHorizon"),Group(root,"Medium_OrbitalGarden"),Group(root,"Hard_SolarReactor")};
        Therapy(groups[0]);Easy(groups[1]);Medium(groups[2]);Hard(groups[3]);
        var so=new SerializedObject(controller);so.FindProperty("gameplay").objectReferenceValue=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();
        var themes=so.FindProperty("themes");themes.arraySize=4;
        for(int i=0;i<4;i++)
        {
            var theme=themes.GetArrayElementAtIndex(i);theme.FindPropertyRelative("root").objectReferenceValue=groups[i].gameObject;
            theme.FindPropertyRelative("reactionOpacity").floatValue=i==0?.045f:i==1?.08f:i==2?.13f:.17f;
            var details=theme.FindPropertyRelative("reactiveDetails");details.arraySize=reactives[i].Count;
            for(int j=0;j<details.arraySize;j++)details.GetArrayElementAtIndex(j).objectReferenceValue=reactives[i][j];
            var accents=theme.FindPropertyRelative("hitAccents");accents.arraySize=6;
            for(int j=0;j<6;j++)
            {
                var image=Image(groups[i],"HitAccent"+j,Vfx+(i<=1?"FX_Circle_Soft.png":i==2?"FX_Star.png":"FX_Shine_Thin.png"),Vector2.zero,i<=1?Vector2.one*75:i==2?Vector2.one*28:new Vector2(10,65),Color.white);
                image.enabled=false;var accent=image.gameObject.AddComponent<VisualLevelHitAccent>();var ac=new SerializedObject(accent);
                ac.FindProperty("controller").objectReferenceValue=controller;ac.FindProperty("graphic").objectReferenceValue=image;ac.FindProperty("style").enumValueIndex=i<=1?0:i==2?1:2;ac.ApplyModifiedPropertiesWithoutUndo();
                accents.GetArrayElementAtIndex(j).objectReferenceValue=accent;
            }
            groups[i].gameObject.SetActive(i==1);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();var gameConfig=new SerializedObject(game);gameConfig.FindProperty("visualLevels").objectReferenceValue=controller;gameConfig.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Saved four distinct visual environments and separate pooled hit accents: Quiet Studio, Calm Horizon, Orbital Garden, Solar Reactor. Automatic mode/difficulty selection connected.";
    }
}
