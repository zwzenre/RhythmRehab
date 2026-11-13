using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using RhythmRehab;
public static class AuthorClassicalSongs
{
    static readonly string[] Folders={"Beethoven_FurElise","Pachelbel_CanonInD","Bach_PreludeInC","Mozart_SonataK545_Rondo","Chopin_PreludeInEMinor"};
    static readonly string[] Titles={"Für Elise","Canon in D","Prelude in C","Rondo K.545","Prelude in E minor"};
    static readonly string[] Artists={"Ludwig van Beethoven","Johann Pachelbel","Johann Sebastian Bach","Wolfgang Amadeus Mozart","Frédéric Chopin"};
    static readonly float[] Bpms={144,92,65.9f,87.8f,120};
    static void Bind(SerializedObject s,string name,UnityEngine.Object value)=>s.FindProperty(name).objectReferenceValue=value;
    static SongChart Chart(string folder,string name)
    {
        string root="Assets/Music/Classical/"+folder+"/Charts/",path=root+name+".asset";
        var chart=AssetDatabase.LoadAssetAtPath<SongChart>(path);
        if(!chart){chart=ScriptableObject.CreateInstance<SongChart>();AssetDatabase.CreateAsset(chart,path);}
        JsonUtility.FromJsonOverwrite(File.ReadAllText(root+name+".json"),chart);EditorUtility.SetDirty(chart);return chart;
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(original.isDirty)throw new Exception("Save current scene first.");string originalPath=original.path;
        AssetDatabase.Refresh();
        var catalog=AssetDatabase.LoadAssetAtPath<SongCatalog>("Assets/Data/ClassicalSongs.asset");
        if(!catalog){catalog=ScriptableObject.CreateInstance<SongCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/Data/ClassicalSongs.asset");}
        var songs=new System.Collections.Generic.List<SongEntry>();
        for(int i=0;i<Folders.Length;i++)
        {
            string folder="Assets/Music/Classical/"+Folders[i],audioPath=Directory.GetFiles(folder,"*.mp3").Single().Replace('\\','/');
            var audioImporter=(AudioImporter)AssetImporter.GetAtPath(audioPath);var sample=audioImporter.defaultSampleSettings;
            if(sample.loadType!=AudioClipLoadType.CompressedInMemory||sample.compressionFormat!=AudioCompressionFormat.Vorbis||Math.Abs(sample.quality-.85f)>.001f||!sample.preloadAudioData)
            { sample.loadType=AudioClipLoadType.CompressedInMemory;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.85f;sample.preloadAudioData=true;audioImporter.defaultSampleSettings=sample;audioImporter.SaveAndReimport(); }
            var coverImporter=(TextureImporter)AssetImporter.GetAtPath(folder+"/Cover.jpg");
            if(coverImporter.textureType!=TextureImporterType.Sprite||coverImporter.spriteImportMode!=SpriteImportMode.Single||coverImporter.mipmapEnabled||coverImporter.maxTextureSize!=1024)
            { coverImporter.textureType=TextureImporterType.Sprite;coverImporter.spriteImportMode=SpriteImportMode.Single;coverImporter.mipmapEnabled=false;coverImporter.maxTextureSize=1024;coverImporter.SaveAndReimport(); }
            songs.Add(new SongEntry{id="classical-"+Folders[i].ToLowerInvariant(),title=Titles[i],artist=Artists[i],bpm=Bpms[i],
                audio=AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath),audioLeadInSeconds=4,recordingCredit="amclassical.com",previewStartSeconds=i==1?32:8,previewDurationSeconds=20,
                cover=AssetDatabase.LoadAllAssetsAtPath(folder+"/Cover.jpg").OfType<Sprite>().First(),coverColor=Color.white,
                therapyChart=Chart(Folders[i],"Therapy"),easyChart=Chart(Folders[i],"Easy"),mediumChart=Chart(Folders[i],"Medium"),hardChart=Chart(Folders[i],"Hard")});
        }
        catalog.songs=songs.ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/SongSelection.unity");var controller=UnityEngine.Object.FindAnyObjectByType<SongSelectionController>();
        catalog=AssetDatabase.LoadAssetAtPath<SongCatalog>("Assets/Data/ClassicalSongs.asset");
        var content=GameObject.Find("/Canvas/SongScroll/Viewport/Content").transform;
        for(int i=content.childCount;i<songs.Count;i++){var clone=UnityEngine.Object.Instantiate(content.GetChild(0).gameObject,content,false);clone.name="Song"+i;}
        var config=new SerializedObject(controller);Bind(config,"catalog",catalog);var cards=config.FindProperty("cards");cards.arraySize=songs.Count;
        for(int i=0;i<songs.Count;i++)
        {
            var root=content.Find("Song"+i);root.gameObject.SetActive(true);var view=root.GetComponent<SongCardView>();var s=new SerializedObject(view);
            Bind(s,"owner",controller);s.FindProperty("songIndex").intValue=i;s.ApplyModifiedPropertiesWithoutUndo();
            var button=root.GetComponent<UnityEngine.UI.Button>();button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,view.Select);
            foreach(var text in root.Find("Cover").GetComponentsInChildren<TMP_Text>(true))if(text.text.All(char.IsDigit))text.text=(i+1).ToString("00");
            view.Bind(songs[i]);view.SetSelected(i==0);EditorUtility.SetDirty(view);cards.GetArrayElementAtIndex(i).objectReferenceValue=view;
        }
        config.ApplyModifiedPropertiesWithoutUndo();
        for(int i=content.childCount-1;i>=songs.Count;i--)Undo.DestroyObjectImmediate(content.GetChild(i).gameObject);
        GameObject.Find("/Canvas/BrowseLabel").GetComponent<TMP_Text>().text=songs.Count+" SONGS · DRAG OR SCROLL TO BROWSE";
        var selection=GameObject.Find("/Canvas/SelectionLabel").GetComponent<TMP_Text>();selection.text="SELECTED: Für Elise\nRecording: amclassical.com";selection.fontSize=27;
        Canvas.ForceUpdateCanvases();UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
        GameObject.Find("/Canvas/SongScroll").GetComponent<UnityEngine.UI.ScrollRect>().horizontalNormalizedPosition=0;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        foreach(string name in new[]{"ModeSelection","ExerciseSelection","DifficultySelection"})
        {
            scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var page=UnityEngine.Object.FindAnyObjectByType<SelectionPageController>();var s=new SerializedObject(page);Bind(s,"catalog",AssetDatabase.LoadAssetAtPath<SongCatalog>("Assets/Data/ClassicalSongs.asset"));s.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        EditorSceneManager.OpenScene(originalPath);
        return "Saved exactly 5 classical songs and cards, 20 editable SongChart assets, covers and credits. Removed all placeholder cards; all selection pages use the five-song catalogue.";
    }
}
