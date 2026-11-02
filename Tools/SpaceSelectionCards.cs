using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SpaceSelectionCards
{
 static RectTransform R(Transform parent,string name)
 {var t=parent.Find(name);if(!t)throw new Exception("Missing layout object: "+name);return (RectTransform)t;}
 static void Set(RectTransform rect,float x,float y,float w,float h)
 {Undo.RecordObject(rect,"Add breathing room to selection cards");rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(w,h);}
 static void Rows(Transform root,bool mode)
 {
  var rows=root.Cast<Transform>().Where(t=>t.name.EndsWith("Feature")).ToArray();
  if(rows.Length!=3)throw new Exception("Expected three existing feature rows.");
  for(int i=0;i<rows.Length;i++)
  {
   var row=(RectTransform)rows[i];float width=mode?650:430,height=mode?84:64;
   Set(row,0,mode?70-i*112:-i*90,width,height);
   var accent=R(row,"Accent");Set(accent,-width/2+2,0,3,height-20);
   var tile=R(row,"IconTile");Set(tile,-width/2+(mode?40:32),0,mode?48:34,mode?48:34);
   float left=-width/2+(mode?90:70),right=width/2-16,x=(left+right)*.5f,w=right-left;
   Set(R(row,"Heading"),x,mode?18:17,w,mode?24:20);
   Set(R(row,"Detail"),x,mode?-14:-13,w,mode?35:30);
  }
 }
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
  var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string original=active.path;
  if(active.isDirty)EditorSceneManager.SaveScene(active);
  foreach(var name in new[]{"ModeSelection","ExerciseSelection","DifficultySelection"})
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var canvas=GameObject.Find("/Canvas").transform;
   Set(R(canvas,"SongContext"),0,285,1600,60);
   bool mode=name=="ModeSelection";
   foreach(var cardName in mode?new[]{"TherapyCard","FreeCard"}:new[]{"Choice0","Choice1","Choice2"})
   {
    var card=R(canvas,cardName);Set(card,card.anchoredPosition.x,-65,mode?760:500,600);
    Set(R(card,"Title"),0,215,mode?680:445,mode?80:85);
    var divider=R(card,"Divider");Set(divider,0,mode?145:65,mode?650:425,2);
    var root=R(card,"DescriptionDesign");Rows(root,mode);
    if(mode)
    {
     Set(R(card,"ModeIcon"),-290,215,48,48);
     Set(R(root,"ModeBadge"),0,-258,650,40);
    }
    else
    {
     string indicator=name=="ExerciseSelection"?"HeightIndicator":"DifficultyStars";
     Set(R(card,indicator),0,125,430,70);
     Set(R(card,"SelectedBadge"),0,-263,240,45);
    }
   }
   if(mode)
   {
    Set(R(canvas,"InfoPanel"),0,-450,1620,82);Set(R(canvas,"Info"),0,-450,1520,70);
   }
   else Set(R(canvas,"SelectedLabel"),-235,-450,1100,95);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  EditorSceneManager.OpenScene(original);return "Spacing updated in all three scenes: taller cards, more separation between feature rows, extra text/icon padding, and larger gaps around headers and badges. Content and styling unchanged.";
 }
}
