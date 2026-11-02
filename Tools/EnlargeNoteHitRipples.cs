using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RhythmRehab;
public static class EnlargeNoteHitRipples
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        var original=SceneManager.GetActiveScene();
        if(original.isDirty)throw new Exception("Save the current scene first.");
        string path=original.path;
        try
        {
            foreach(string name in new[]{"Gameplay","RhythmCalibration"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var ripple=UnityEngine.Object.FindAnyObjectByType<NoteHitRipples>();
                if(!ripple)throw new Exception("Ripple missing in "+name);
                var rect=ripple.GetComponent<RectTransform>();
                rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
                rect.offsetMin=rect.offsetMax=Vector2.zero;
                rect.SetAsFirstSibling();
                if(!ripple.GetComponent<UnityEngine.UI.RectMask2D>())ripple.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                var mask=ripple.GetComponent<UnityEngine.UI.RectMask2D>();
                // Let waves pass behind the HUD text, while keeping bottom controls clear.
                mask.padding=name=="Gameplay"?new Vector4(0,150,0,0):Vector4.zero;
                mask.softness=name=="Gameplay"?new Vector2Int(0,24):Vector2Int.zero;
                if(name=="Gameplay")
                {
                    foreach(string card in new[]{"ScoreCard","AccuracyCard"})
                    {
                        var panel=GameObject.Find("/Canvas/"+card);
                        if(!panel)throw new Exception("HUD panel missing: "+card);
                        var image=panel.GetComponent<UnityEngine.UI.Image>();
                        if(image){image.enabled=false;image.raycastTarget=false;}
                    }
                }
                var config=new SerializedObject(ripple);
                config.FindProperty("viewport").objectReferenceValue=ripple.GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();
                config.FindProperty("minimumRightwardReach").floatValue=.7f;
                config.FindProperty("duration").floatValue=1.15f;
                config.FindProperty("opacity").floatValue=.28f;
                config.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
        }
        finally{if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);}
        return "Gameplay and calibration: 70% screen-width final radius, 1.15 seconds, 28% initial opacity fading to zero, behind notes and clipped to playfield.";
    }
}
