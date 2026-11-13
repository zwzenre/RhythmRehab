using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class AnalyseClassicalAudio
{
    public static string Main()=>Analyse(new[]{"Beethoven_FurElise","Pachelbel_CanonInD","Bach_PreludeInC","Mozart_SonataK545_Rondo","Chopin_PreludeInEMinor"});
    public static string NewSongs()=>Analyse(new[]{"Mozart_SonataK545_Rondo","Chopin_PreludeInEMinor"});
    static string Analyse(string[] folders)
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before analysing audio.");
        AssetDatabase.Refresh();var report=new System.Collections.Generic.List<string>();
        foreach(string folder in folders)
        {
            string path=Directory.GetFiles("Assets/Music/Classical/"+folder,"*.mp3").Single().Replace('\\','/');
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);clip.LoadAudioData();
            var samples=new float[clip.samples*clip.channels];if(!clip.GetData(samples,0))throw new Exception("Audio data unavailable: "+folder);
            int hop=Mathf.Max(1,clip.frequency/200),count=clip.samples/hop;var energy=new double[count];
            for(int i=0;i<count;i++){double e=0;for(int j=0;j<hop;j++)for(int c=0;c<clip.channels;c++){double s=samples[((i*hop+j)*clip.channels)+c];e+=s*s;}energy[i]=Math.Sqrt(e/(hop*clip.channels));}
            File.WriteAllText("Temp/ClassicalAnalysis/"+folder+"-audio.json","{\"duration\":"+clip.length.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"rate\":"+((double)clip.frequency/hop).ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"energy\":["+string.Join(",",energy.Select(v=>v.ToString("G6",System.Globalization.CultureInfo.InvariantCulture)))+"]}");
            report.Add(folder+": "+clip.length+" seconds, "+clip.frequency+" Hz, "+clip.channels+" channels");
        }
        return string.Join("\n",report);
    }
}

