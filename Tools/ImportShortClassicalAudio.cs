using System;
using System.IO;
using UnityEditor;
public static class ImportShortClassicalAudio
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        string[] folders={"Beethoven_FurElise","Pachelbel_CanonInD"};string[] files={"FurElise.mp3","CanonInD.mp3"};
        for(int i=0;i<folders.Length;i++)
        {
            string root="Assets/Music/Classical/"+folders[i],sourceFolder=root+"/Sources",path=root+"/"+files[i],original=sourceFolder+"/Original.mp3";
            if(File.Exists(original))throw new Exception("Original already backed up: "+original);
            if(!File.Exists("Temp/ShortClassical/"+folders[i]+".mp3"))throw new Exception("Encoded excerpt missing.");
            if(!AssetDatabase.IsValidFolder(sourceFolder))AssetDatabase.CreateFolder(root,"Sources");
            string error=AssetDatabase.MoveAsset(path,original);if(!string.IsNullOrEmpty(error))throw new Exception(error);
            File.Copy("Temp/ShortClassical/"+folders[i]+".mp3",path,false);
            File.Copy("Temp/ShortClassical/"+folders[i]+".json",root+"/AudioEdit.json",false);
        }
        AssetDatabase.Refresh();return "Imported shortened MP3s. Unchanged downloaded recordings preserved as Sources/Original.mp3 in each song folder.";
    }
}
