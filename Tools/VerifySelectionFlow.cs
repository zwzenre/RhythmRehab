using System;
using System.Linq;
using UnityEditor;
using RhythmRehab;

public static class VerifySelectionFlow
{
    public static string Main()
    {
        int checks=0;Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);checks++;};
        var catalog=AssetDatabase.LoadAssetAtPath<SongCatalog>("Assets/Data/PlaceholderSongs.asset");
        check(catalog!=null && catalog.songs.Length==8,"Eight placeholder songs");
        check(catalog.songs.Select(s=>s.id).Distinct().Count()==8,"Unique song IDs");
        foreach(var song in catalog.songs)check(song.bpm>=60 && song.bpm<=180,"Valid tempo");
        var selected=catalog.songs[5];
        foreach(HeightLevel height in Enum.GetValues(typeof(HeightLevel)))
        {
            var p=SessionSelection.BuildProfile(selected,SessionMode.Therapy,height,FreeDifficulty.Hard);
            check(p.chart.Length==32 && p.chart.All(n=>n.lane==(int)height),"Consistent therapy height "+height);
            check(p.chart.All(n=>!n.useCustomHeight&&n.TargetHeight==(int)height*.5f),"Therapy uses exact selected height");
            check(p.bpm==140,"Selected tempo preserved");
        }
        int previous=0;double previousWindow=2;
        foreach(FreeDifficulty difficulty in Enum.GetValues(typeof(FreeDifficulty)))
        {
            var p=SessionSelection.BuildProfile(selected,SessionMode.Free,HeightLevel.High,difficulty);
            check(p.chart.Length>previous && p.chart.Length<=32,"Difficulty note count");previous=p.chart.Length;
            check(p.timingScale<previousWindow,"Difficulty timing windows");previousWindow=p.timingScale;
            check(p.chart.Select(n=>n.lane).Distinct().Count()>=2,"Mixed free heights");
            check(difficulty!=FreeDifficulty.Easy||p.chart.All(n=>n.lane<2),"Easy lower heights");
            check(p.chart.All(n=>n.useCustomHeight&&n.TargetHeight>=0&&n.TargetHeight<=1),"Valid continuous free heights");
            check(p.chart.Any(n=>Math.Abs(n.TargetHeight*2-Math.Round(n.TargetHeight*2))>.01),"Free notes between guide lines");
            check(difficulty!=FreeDifficulty.Easy||p.chart.All(n=>n.TargetHeight<=.5f),"Easy stays in lower half");
            check(difficulty==FreeDifficulty.Easy||p.chart.Any(n=>n.TargetHeight>.5f&&n.TargetHeight<1),"Upper intermediate heights");
            check(p.chart.Any(n=>n.TargetHeight==.25f),"Exact halfway LOW–MID notes");
            check(difficulty==FreeDifficulty.Easy||p.chart.Any(n=>n.TargetHeight==.75f),"Exact halfway MID–HIGH notes");
            for(int i=1;i<p.chart.Length;i++)check(p.chart[i].beat>p.chart[i-1].beat,"Ordered notes");
        }
        check(RhythmRules.Judge(.25,0,.14f,1.3)==HitJudgement.Good,"Easy wider timing");
        check(RhythmRules.Judge(.2,0,.14f,.8)==HitJudgement.Miss,"Hard tighter timing");
        check(new ChartNote{lane=1}.TargetHeight==.5f,"Existing authored charts retain fixed lane heights");
        check(new ChartNote{lane=2,useCustomHeight=true,customHeight=.24f}.TargetHeight==.24f,"Intermediate height overrides lane");
        SessionSelection.SelectSong(selected);SessionSelection.SelectMode(SessionMode.Therapy);SessionSelection.SelectHeight(HeightLevel.High);SessionSelection.Confirm();
        check(SessionSelection.Configured && SessionSelection.Song==selected && SessionSelection.Height==HeightLevel.High,"Confirmed therapy choices");
        SessionSelection.BeginBrowsing();check(!SessionSelection.Configured && SessionSelection.Song==selected,"New browsing clears configuration and retains song");
        return checks+" song, therapy, free difficulty and timing checks passed";
    }
}
