using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class PrepareShortClassicalAudio
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        string[] folders={"Beethoven_FurElise","Pachelbel_CanonInD"};string[] files={"FurElise.mp3","CanonInD.mp3"};
        float[] ends={123.292f,120f},fades={2f,3.09f};Directory.CreateDirectory("Temp/ShortClassical");
        for(int s=0;s<folders.Length;s++)
        {
            string path="Assets/Music/Classical/"+folders[s]+"/"+files[s];
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);clip.LoadAudioData();var samples=new float[clip.samples*clip.channels];if(!clip.GetData(samples,0))throw new Exception("Cannot decode "+path);
            int frames=Mathf.Min(clip.samples,Mathf.RoundToInt(ends[s]*clip.frequency));
            using(var writer=new BinaryWriter(File.Create("Temp/ShortClassical/"+folders[s]+".pcm")))
                for(int frame=0;frame<frames;frame++)
                {
                    float left=(frames-frame)/(float)clip.frequency;
                    float gain=Mathf.SmoothStep(0,1,Mathf.Clamp01(left/fades[s]));
                    for(int ch=0;ch<clip.channels;ch++)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(samples[frame*clip.channels+ch]*gain,-1,1)*32767));
                }
            File.WriteAllText("Temp/ShortClassical/"+folders[s]+".json","{\"frequency\":"+clip.frequency+",\"channels\":"+clip.channels+",\"frames\":"+frames+",\"sourceEndSeconds\":"+ends[s].ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"fadeOutSeconds\":"+fades[s].ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
        }
        return "Decoded both original recordings to temporary PCM and applied phrase-aligned end fades before MP3 encoding.";
    }
}
