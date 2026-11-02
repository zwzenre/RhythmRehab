using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RhythmRehab;

public static class AuthorNoteHitRipples
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode first.");
        var original = SceneManager.GetActiveScene();
        if (original.isDirty) throw new Exception("Current scene has unsaved changes.");
        string path = original.path;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/BoldVFXPackDemo/Content/Atlas/Sprites/FX_Circle_Hollow.png");
        if (!texture) throw new Exception("Hollow circle texture not found.");
        var report = new List<string>();
        try
        {
            foreach (string sceneName in new[] { "Gameplay", "RhythmCalibration" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity");
                MonoBehaviour controller = sceneName == "Gameplay" ? (MonoBehaviour)UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>() : UnityEngine.Object.FindAnyObjectByType<RhythmCalibrationController>();
                if (!controller) throw new Exception("Controller missing: " + sceneName);
                var controllerConfig = new SerializedObject(controller);
                var effects = controllerConfig.FindProperty("hitEffects").objectReferenceValue as NoteHitEffects;
                if (!effects) throw new Exception("Note hit effects missing: " + sceneName);
                var group = effects.transform.parent.Find("NoteHitRipples") as RectTransform;
                if (!group)
                {
                    group = new GameObject("NoteHitRipples", typeof(RectTransform)).GetComponent<RectTransform>();
                    group.SetParent(effects.transform.parent, false);
                }
                group.anchorMin = group.anchorMax = new Vector2(.5f, .5f);
                group.pivot = new Vector2(.5f,.5f);
                group.anchoredPosition = Vector2.zero;
                group.sizeDelta = Vector2.zero;
                group.SetSiblingIndex(effects.transform.GetSiblingIndex());
                var ripple = group.GetComponent<NoteHitRipples>();
                if (!ripple) ripple = group.gameObject.AddComponent<NoteHitRipples>();
                var config = new SerializedObject(ripple);
                var rings = config.FindProperty("rings");
                rings.arraySize = 8;
                for (int i = 0; i < 8; i++)
                {
                    var child = group.Find("Ring" + (i+1));
                    var image = child ? child.GetComponent<UnityEngine.UI.RawImage>() : new GameObject("Ring"+(i+1), typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
                    image.transform.SetParent(group, false);
                    image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f,.5f);
                    image.rectTransform.anchoredPosition = Vector2.zero;
                    image.rectTransform.sizeDelta = Vector2.one * 24;
                    image.texture = texture;
                    image.raycastTarget = false;
                    image.color = Color.white;
                    image.enabled = false;
                    rings.GetArrayElementAtIndex(i).objectReferenceValue = image;
                }
                config.ApplyModifiedPropertiesWithoutUndo();
                var effectsConfig = new SerializedObject(effects);
                effectsConfig.FindProperty("ripples").objectReferenceValue = ripple;
                effectsConfig.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.Add(sceneName + ": 8 saved rings, hit effects bound, raycasts disabled.");
            }
        }
        finally { if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path); }
        return string.Join("\n", report);
    }
}
