using System.Linq;
using RhythmRehab;
using UnityEditor;

public static class FinalizeSelectionFlow
{
    public static string Main()
    {
        string[] paths={"MainMenu","SongSelection","ModeSelection","ExerciseSelection","DifficultySelection","Gameplay","Settings","Results"};
        var ordered=paths.Select(s=>new EditorBuildSettingsScene("Assets/Scenes/"+s+".unity",true)).ToList();
        ordered.AddRange(EditorBuildSettings.scenes.Where(s=>!ordered.Any(o=>o.path==s.path)&&System.IO.File.Exists(s.path)));
        EditorBuildSettings.scenes=ordered.ToArray();
        var catalog=AssetDatabase.LoadAssetAtPath<SongCatalog>("Assets/Data/PlaceholderSongs.asset");
        SessionSelection.SelectSong(catalog.songs[0]);SessionSelection.SelectMode(SessionMode.Therapy);SessionSelection.SelectHeight(HeightLevel.Low);SessionSelection.SelectDifficulty(FreeDifficulty.Easy);SessionSelection.BeginBrowsing();
        return "Eight flow scenes enabled; MainMenu remains the entry scene";
    }
}
