using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RhythmRehab;
public static class RestyleSpaceVisualLevels
{
    const string Kit="Assets/Assets/Space_Exploration_GUI_Kit/";
    const string Vfx="Assets/Assets/BoldVFXPackDemo/Content/Atlas/Sprites/";
    static GameplayVisualLevels controller;
    static readonly List<UnityEngine.UI.Graphic>[] details={new(),new(),new(),new()};
    static UnityEngine.UI.RawImage Image(Transform root,string name,string asset,Vector2 pos,Vector2 size,Color colour)
    {
        var child=root.Find(name);UnityEngine.UI.RawImage image;
        if(child)image=child.GetComponent<UnityEngine.UI.RawImage>();
        else
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.RawImage));go.transform.SetParent(root,false);image=go.GetComponent<UnityEngine.UI.RawImage>();
        }
        image.rectTransform.anchorMin=image.rectTransform.anchorMax=new Vector2(.5f,.5f);image.rectTransform.anchoredPosition=pos;image.rectTransform.sizeDelta=size;
        image.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(asset);if(!image.texture)throw new Exception("Texture missing: "+asset);
        image.color=colour;image.raycastTarget=false;return image;
    }
    static void Motion(Component target,VisualLevelMotion.Motion mode,Vector2 amplitude,float speed,float phase=0)
    {
        var motion=target.GetComponent<VisualLevelMotion>();if(!motion)motion=target.gameObject.AddComponent<VisualLevelMotion>();
        var so=new SerializedObject(motion);so.FindProperty("controller").objectReferenceValue=controller;so.FindProperty("motion").enumValueIndex=(int)mode;
        so.FindProperty("amplitude").vector2Value=amplitude;so.FindProperty("speed").floatValue=speed;so.FindProperty("phase").floatValue=phase;so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Remove(Transform root,params string[] prefixes)
    {
        foreach(var child in root.Cast<Transform>().ToArray())if(prefixes.Any(p=>child.name.StartsWith(p,StringComparison.Ordinal)))UnityEngine.Object.DestroyImmediate(child.gameObject);
    }
    static UnityEngine.UI.RawImage Glow(Transform root,int index,string name,Vector2 pos,Vector2 size,Color colour,float speed=.15f)
    {
        var image=Image(root,name,Vfx+"FX_Circle_Soft.png",pos,size,colour);Motion(image,VisualLevelMotion.Motion.Float,new Vector2(65,32),speed,index*.7f);details[index].Add(image);return image;
    }
    static void Ribbon(Transform root,int index,string name,Vector2 pos,Vector2 size,Color colour,float angle,float speed)
    {
        var image=Image(root,name,Vfx+"FX_Circle_Soft.png",pos,size,colour);image.rectTransform.localRotation=Quaternion.Euler(0,0,angle);
        Motion(image,VisualLevelMotion.Motion.Float,new Vector2(110,32),speed,angle);details[index].Add(image);
    }
    static void Stars(Transform root,int index,int count,Color colour,float speed)
    {
        for(int i=0;i<count;i++)
        {
            float size=i%6==0?9:3+i%3;
            var image=Image(root,"SpaceDust"+(i+1),i%6==0?Kit+"Other_Images/Small/sparkle-small.png":Vfx+"FX_Circle_Soft.png",
                new Vector2(-520+Mathf.Repeat(i*337.1f,1450),-340+Mathf.Repeat(i*157.3f,685)),Vector2.one*size,colour);
            Motion(image,VisualLevelMotion.Motion.Drift,new Vector2(0,8),speed+i%5*2,i*.9f);if(i%4==0)details[index].Add(image);
        }
    }
    static void Trails(Transform root,int index,int count,Color colour,float speed)
    {
        for(int i=0;i<count;i++)
        {
            var image=Image(root,"MeteorTrail"+i,Kit+"Other_Images/Large/shooting-star-"+(i%2==0?"blue":"pink")+"-large.png",
                new Vector2(-250+Mathf.Repeat(i*371.5f,1150),-300+Mathf.Repeat(i*233.7f,610)),new Vector2(190+i%3*40,70),colour);
            Motion(image,VisualLevelMotion.Motion.Drift,new Vector2(0,20),speed+i*7,i);details[index].Add(image);
        }
    }
    static void Corona(Transform root,int index,int number)
    {
        string name="SolarCorona"+number;var child=root.Find(name);
        var path=child?child.GetComponent<VisualLevelPath>():new GameObject(name,typeof(RectTransform),typeof(VisualLevelPath)).GetComponent<VisualLevelPath>();
        path.transform.SetParent(root,false);var rect=path.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(680,170);rect.sizeDelta=new Vector2(1920,1080);
        path.color=new Color(1,.52f,.15f,.12f);path.raycastTarget=false;
        var so=new SerializedObject(path);var points=so.FindProperty("points");points.arraySize=46;
        for(int i=0;i<46;i++){float a=Mathf.Lerp(20+number*40,265+number*20,i/45f)*Mathf.Deg2Rad;points.GetArrayElementAtIndex(i).vector2Value=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(145+number*55);}
        so.FindProperty("lineWidth").floatValue=2;so.ApplyModifiedPropertiesWithoutUndo();
        Motion(path,VisualLevelMotion.Motion.Rotate,Vector2.zero,number%2==0?3:-4);details[index].Add(path);
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Save current scene first.");
        if(scene.name!="Gameplay")scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
        controller=UnityEngine.Object.FindAnyObjectByType<GameplayVisualLevels>();if(!controller)throw new Exception("Visual level controller missing.");
        var config=new SerializedObject(controller);var themes=config.FindProperty("themes");
        var roots=Enumerable.Range(0,4).Select(i=>((GameObject)themes.GetArrayElementAtIndex(i).FindPropertyRelative("root").objectReferenceValue).transform).ToArray();
        for(int i=0;i<4;i++)
        {
            var existing=themes.GetArrayElementAtIndex(i).FindPropertyRelative("reactiveDetails");
            for(int j=0;j<existing.arraySize;j++){var graphic=existing.GetArrayElementAtIndex(j).objectReferenceValue as UnityEngine.UI.Graphic;if(graphic)details[i].Add(graphic);}
        }
        Remove(roots[0],"OpenStudioArc","CalmContour");
        Remove(roots[1],"DistantHills","NearHills","SoftCloud","WaterContour","Moon","Firefly");
        Remove(roots[3],"ReactorHexagon","CircuitTrace","HoneycombCell","EnergyChevron");
        foreach(var list in details)list.RemoveAll(g=>!g);
        roots[0].name="Therapy_QuietSpace";roots[1].name="Easy_BlueNebula";roots[2].name="Medium_VioletOrbit";roots[3].name="Hard_SolarStorm";

        Glow(roots[0],0,"QuietBlueLight",new Vector2(640,-220),new Vector2(1250,950),new Color(.08f,.4f,.8f,.07f),.1f);
        Glow(roots[0],0,"QuietVioletNebula",new Vector2(-50,280),new Vector2(1150,850),new Color(.32f,.12f,.65f,.065f),.09f);
        Ribbon(roots[0],0,"QuietStarlightRibbon",new Vector2(210,160),new Vector2(1800,80),new Color(.16f,.48f,.9f,.04f),12,.1f);
        var distant=Image(roots[0],"DistantPlanet",Kit+"Icons/exoplanet-3-512.png",new Vector2(780,245),Vector2.one*120,new Color(.48f,.7f,1,.16f));
        Motion(distant,VisualLevelMotion.Motion.Float,new Vector2(12,8),.1f);Stars(roots[0],0,20,new Color(.45f,.65f,1,.25f),7);

        var oldGlow=roots[1].Find("HorizonGlow");if(oldGlow)oldGlow.name="BlueNebula";
        Glow(roots[1],1,"BlueNebula",new Vector2(690,-240),new Vector2(1250,950),new Color(.08f,.4f,1,.1f));
        Glow(roots[1],1,"VioletNebula",new Vector2(-120,270),new Vector2(1250,1000),new Color(.38f,.12f,.85f,.1f));
        Glow(roots[1],1,"TealNebula",new Vector2(630,360),new Vector2(950,650),new Color(.06f,.7f,.65f,.065f));
        Ribbon(roots[1],1,"BlueLightRibbon",new Vector2(350,-190),new Vector2(1750,85),new Color(.1f,.55f,1,.06f),-12,.16f);
        Ribbon(roots[1],1,"VioletLightRibbon",new Vector2(100,230),new Vector2(1800,100),new Color(.4f,.2f,1,.055f),15,.14f);
        var bluePlanet=Image(roots[1],"BluePlanet",Kit+"Icons/exoplanet-3-512.png",new Vector2(770,230),Vector2.one*180,new Color(.5f,.7f,1,.17f));
        Motion(bluePlanet,VisualLevelMotion.Motion.Float,new Vector2(20,12),.14f);Stars(roots[1],1,32,new Color(.35f,.65f,1,.3f),15);

        Glow(roots[2],2,"MagentaNebula",new Vector2(-50,260),new Vector2(1200,900),new Color(.6f,.08f,.65f,.075f),.17f);
        Ribbon(roots[2],2,"VioletStarlightRibbon",new Vector2(160,-140),new Vector2(1850,110),new Color(.45f,.2f,1,.065f),-14,.2f);
        Ribbon(roots[2],2,"PinkStarlightRibbon",new Vector2(400,290),new Vector2(1500,75),new Color(.85f,.2f,.65f,.05f),13,.18f);
        Trails(roots[2],2,2,new Color(.85f,.6f,1,.17f),28);

        var reactor=roots[3].Find("ReactorGlow");if(reactor)reactor.name="SolarGlow";
        Glow(roots[3],3,"SolarGlow",new Vector2(680,170),Vector2.one*1000,new Color(1,.26f,.05f,.09f),.2f);
        Glow(roots[3],3,"CrimsonNebula",new Vector2(-100,-220),new Vector2(1250,900),new Color(.8f,.04f,.2f,.08f),.18f);
        Glow(roots[3],3,"VioletStormCloud",new Vector2(460,-310),new Vector2(1300,700),new Color(.45f,.07f,.7f,.07f),.22f);
        var sun=Image(roots[3],"SolarStar",Kit+"Icons/sun-512.png",new Vector2(680,170),Vector2.one*200,new Color(1,.72f,.35f,.2f));
        Motion(sun,VisualLevelMotion.Motion.Rotate,Vector2.zero,1.5f);for(int i=0;i<3;i++)Corona(roots[3],3,i);
        Ribbon(roots[3],3,"SolarFlareRibbon",new Vector2(180,190),new Vector2(1800,90),new Color(1,.3f,.15f,.055f),-18,.27f);
        Stars(roots[3],3,36,new Color(1,.6f,.4f,.27f),28);Trails(roots[3],3,6,new Color(1,.65f,.5f,.19f),58);

        for(int i=0;i<4;i++)
        {
            var field=themes.GetArrayElementAtIndex(i).FindPropertyRelative("reactiveDetails");var values=details[i].Where(g=>g).Distinct().ToArray();field.arraySize=values.Length;
            for(int j=0;j<values.Length;j++)field.GetArrayElementAtIndex(j).objectReferenceValue=values[j];
            roots[i].gameObject.SetActive(i==1);
        }
        config.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "All levels now share the moving space atmosphere: Quiet Space, Blue Nebula, Violet Orbit, Solar Storm. Hills, water, studio contours and circuits removed; gameplay, tracker and hit ripples preserved.";
    }
}
