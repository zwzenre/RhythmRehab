using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEditor;
public static class AnalyseClassicalAudio
{
    public static string Main() => Analyse(new[] {
        "Beethoven_FurElise", "Pachelbel_CanonInD", "Bach_PreludeInC",
        "Mozart_SonataK545_Rondo", "Chopin_PreludeInEMinor"
    });

    public static string NewSongs() => Analyse(new[] { "Mozart_SonataK545_Rondo", "Chopin_PreludeInEMinor" });

    private static string Analyse(string[] folders)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Play Mode must be stopped.");
        const string scratch = "Assets/__ChartAttackAnalysis";
        if (AssetDatabase.IsValidFolder(scratch)) throw new Exception("Analysis scratch folder already exists.");
        Directory.CreateDirectory("Temp/ClassicalAnalysis");
        AssetDatabase.CreateFolder("Assets", "__ChartAttackAnalysis");
        try
        {
            foreach (var folder in folders)
            {
                var original = Directory.GetFiles("Assets/Music/Classical/"+folder,"*.mp3").Single().Replace('\\','/');
                string copy = scratch + "/" + folder + ".mp3";
                if (!AssetDatabase.CopyAsset(original, copy)) throw new Exception("Could not copy " + original);
                var importer = (AudioImporter)AssetImporter.GetAtPath(copy);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(copy);
                clip.LoadAudioData();
                var samples = new float[clip.samples*clip.channels];
                if (!clip.GetData(samples,0)) throw new Exception("PCM unavailable for " + folder);
                int hop = Mathf.Max(1, Mathf.RoundToInt(clip.frequency / 200f));
                var dump = new StringBuilder("{\"rate\":" + ((double)clip.frequency/hop).ToString("R",CultureInfo.InvariantCulture) +
                    ",\"duration\":" + clip.length.ToString("R",CultureInfo.InvariantCulture) + ",\"energy\":[");
                float preceding = 0;
                for (int offset=0; offset+hop<=clip.samples; offset+=hop)
                {
                    double energy=0, edge=0;
                    for (int index=offset; index<offset+hop; index++)
                    {
                        float sample=0;
                        for(int channel=0;channel<clip.channels;channel++) sample+=samples[index*clip.channels+channel];
                        sample/=clip.channels;
                        energy+=sample*sample;
                        float change=sample-preceding;
                        edge+=change*change;
                        preceding=sample;
                    }
                    if(offset!=0)dump.Append(',');
                    dump.Append('[').Append(Math.Sqrt(energy/hop).ToString("G6",CultureInfo.InvariantCulture));
                    dump.Append(',').Append(Math.Sqrt(edge/hop).ToString("G6",CultureInfo.InvariantCulture)).Append(']');
                }
                dump.Append("]}");
                File.WriteAllText("Temp/ClassicalAnalysis/"+folder+"-transients.json", dump.ToString());
                AssetDatabase.DeleteAsset(copy);
            }
        }
        finally { AssetDatabase.DeleteAsset(scratch); }
        return $"Decoded {folders.Length} shipped recordings for attack alignment.";
    }
}
