using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;
public static class AuthorSeparateAudio
{
    static void Bind(SerializedObject s,string name,UnityEngine.Object value)=>s.FindProperty(name).objectReferenceValue=value;
    static Transform Copy(Transform parent,string source,string name)
    {
        var existing=parent.Find(name);if(existing)return existing;
        var clone=UnityEngine.Object.Instantiate(parent.Find(source).gameObject,parent,false);clone.name=name;return clone.transform;
    }
    static void Y(Transform t,float y){var r=(RectTransform)t;r.anchoredPosition=new Vector2(r.anchoredPosition.x,y);}
    static AudioSource Source(string name,float volume)
    {
        var go=GameObject.Find(name);if(!go)go=new GameObject(name,typeof(AudioSource));
        var source=go.GetComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.loop=false;source.volume=volume;return source;
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        var initial=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(initial.isDirty)throw new Exception("Save current scene first.");
        string original=initial.path;var settings=LocalSettingsStore.Default.Load(out var error);if(error!=null)throw new Exception(error);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Settings.unity");var controller=UnityEngine.Object.FindAnyObjectByType<PrototypeSettings>();var panel=GameObject.Find("/Canvas/Audio").transform;
        ((RectTransform)panel).sizeDelta=new Vector2(1340,300);Y(panel.Find("Title"),120);
        var musicSlider=Copy(panel,"BeatSlider","MusicSlider").GetComponent<UnityEngine.UI.Slider>();var musicLabel=Copy(panel,"BeatLabel","MusicLabel").GetComponent<TMP_Text>();var musicValue=Copy(panel,"BeatValue","MusicValue").GetComponent<TMP_Text>();
        musicLabel.text="Music volume";panel.Find("BeatLabel").GetComponent<TMP_Text>().text="Beat / hit volume";panel.Find("SfxLabel").GetComponent<TMP_Text>().text="UI sound volume";
        foreach(string prefix in new[]{"Music","Beat","Sfx"})
        {
            float y=prefix=="Music"?55:prefix=="Beat"?-15:-85;
            foreach(string suffix in new[]{"Label","Slider","Value"})Y(panel.Find(prefix+suffix),y);
            var slider=panel.Find(prefix+"Slider").GetComponent<UnityEngine.UI.Slider>();slider.onValueChanged=new UnityEngine.UI.Slider.SliderEvent();UnityEventTools.AddPersistentListener(slider.onValueChanged,controller.SaveVolumes);
            float value=prefix=="Music"?settings.musicVolume:prefix=="Beat"?settings.beatVolume:settings.sfxVolume;slider.SetValueWithoutNotify(value);panel.Find(prefix+"Value").GetComponent<TMP_Text>().text=value.ToString("P0");
        }
        var config=new SerializedObject(controller);Bind(config,"musicVolume",musicSlider);Bind(config,"musicValue",musicValue);config.ApplyModifiedPropertiesWithoutUndo();
        GameObject.Find("/Canvas/Footer").GetComponent<TMP_Text>().text="Music, beat / hit and UI sound volumes save automatically.";
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");var gameAudio=UnityEngine.Object.FindAnyObjectByType<GameplayAudio>();config=new SerializedObject(gameAudio);
        var music=Source("SongMusic",settings.musicVolume);var beat=(AudioSource)config.FindProperty("beatSource").objectReferenceValue;music.outputAudioMixerGroup=beat.outputAudioMixerGroup;Bind(config,"musicSource",music);config.ApplyModifiedPropertiesWithoutUndo();
        beat.volume=settings.beatVolume;((AudioSource)config.FindProperty("hitSource").objectReferenceValue).volume=settings.beatVolume;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        scene=EditorSceneManager.OpenScene("Assets/Scenes/SongSelection.unity");var owner=UnityEngine.Object.FindAnyObjectByType<SongSelectionController>();
        var preview=owner.GetComponent<SongPreviewAudio>();if(!preview)preview=owner.gameObject.AddComponent<SongPreviewAudio>();config=new SerializedObject(preview);
        Bind(config,"previewSource",Source("SongPreview",settings.musicVolume));Bind(config,"screenAudio",UnityEngine.Object.FindAnyObjectByType<UIScreenAudio>());config.ApplyModifiedPropertiesWithoutUndo();
        config=new SerializedObject(owner);Bind(config,"songPreview",preview);config.ApplyModifiedPropertiesWithoutUndo();
        var catalog=(SongCatalog)config.FindProperty("catalog").objectReferenceValue;for(int i=0;i<3;i++){catalog.songs[i].previewStartSeconds=i==1?32:8;catalog.songs[i].previewDurationSeconds=20;}
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene(original);
        return "Saved separate Music, Beat/Hit and UI sound sliders; dedicated gameplay music and preview sources; 20-second card-click previews. Existing settings migrated once with quieter beats.";
    }
}
