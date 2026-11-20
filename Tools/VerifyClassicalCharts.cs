using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using RhythmRehab;
public static class VerifyClassicalCharts
{
    public static string Main()
    {
        int checks=0;Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);checks++;};
        var catalog=AssetDatabase.LoadAssetAtPath<SongCatalog>("Assets/Data/ClassicalSongs.asset");check(catalog&&catalog.songs.Length==5&&catalog.songs.All(s=>s.audio&&s.id.StartsWith("classical-")),"Exactly five real classical songs; no placeholders");
        foreach(var song in catalog.songs)
        {
            check(song.audio&&song.cover&&song.recordingCredit=="amclassical.com","Song audio, cover and attribution assigned");
            check(song.audio.length<=150&&song.audio.length+song.audioLeadInSeconds+3.45f<150,"Session under 2:30 including READY, its fade and lead-ins");
            check(song.easyChart.notes.Length<song.mediumChart.notes.Length&&song.mediumChart.notes.Length<song.hardChart.notes.Length,"Increasing difficulty density");
            foreach(var difficulty in new[]{FreeDifficulty.Easy,FreeDifficulty.Medium,FreeDifficulty.Hard})
            {
                var profile=SessionSelection.BuildProfile(song,SessionMode.Free,HeightLevel.Low,difficulty);
                var notes=profile.chart;
                check(notes.Length>32&&notes.All(n=>n.TargetHeight>=0&&n.TargetHeight<=1),"Long chart and normalized heights");
                check(notes.Any(n=>n.IsHold)&&notes.Any(n=>!n.IsHold),"Taps and holds present");
                check(notes.Any(n=>Math.Abs(n.TargetHeight*2-Math.Round(n.TargetHeight*2))>.01),"Intermediate note heights present");
                int closeGaps = 0;
                for (int i = 1; i < notes.Length; i++)
                {
                    float gap = (notes[i].beat - notes[i - 1].beat) * 60 / song.bpm;
                    float recovery = (notes[i].beat - notes[i - 1].EndBeat) * 60 / song.bpm;
                    float minimum = difficulty == FreeDifficulty.Easy ? 1.549f :
                        difficulty == FreeDifficulty.Medium ? .969f : .279f;
                    check(gap >= minimum && (!notes[i - 1].IsHold || recovery >= .319f), "Playable spacing and hold recovery");
                    if (difficulty == FreeDifficulty.Hard)
                    {
                        if (closeGaps == 3) check(gap >= .759f, "Hard bursts leave time to recover");
                        closeGaps = gap < .4499f ? closeGaps + 1 : 0;
                        if (gap < .45f) check(Math.Abs(notes[i].TargetHeight - notes[i - 1].TargetHeight) <= .172f,
                            "Fast Hard notes stay close in height");
                    }
                }
                check(notes.Last().EndBeat*60/song.bpm<profile.durationSeconds&&profile.durationSeconds>=song.audio.length+song.audioLeadInSeconds-.01,"Chart covers full song and includes its tail");
                var summary=new SessionSummary{bpm=song.bpm};var runner=new GameplayChartRunner(notes,song.bpm,profile.approachSeconds,profile.heightTolerance,profile.timingScale,0,.12f,summary,profile.durationSeconds);
                for(int i=0;i<notes.Length;i++)
                {
                    double head=notes[i].beat*runner.SecondsPerBeat;
                    check(runner.TryHit(head,notes[i].TargetHeight),"Every chart head can be hit");
                    if(notes[i].IsHold)check(runner.UpdateHold(notes[i].EndBeat*runner.SecondsPerBeat,notes[i].TargetHeight,true)==HoldOutcome.Completed,"Every authored hold can complete");
                }
                check(summary.perfect==notes.Length&&summary.Judged==notes.Length&&summary.miss==0&&summary.completedHolds==summary.holdNotes,"Full-chart scoring without duplicate or lost judgements");
                check(Math.Abs(runner.EndTime-profile.durationSeconds)<.01,"Countdown follows complete audio duration");
            }
            var original=song.therapyChart.notes.Select(n=>n.TargetHeight).ToArray();
            var therapy = song.therapyChart.notes;
            check(therapy.Any(n => n.IsHold) && therapy.Any(n => !n.IsHold), "Therapy taps and holds present");
            for (int i = 1; i < therapy.Length; i++)
                check((therapy[i].beat - therapy[i - 1].EndBeat) * 60 / song.bpm >= .649f, "Therapy holds leave time to recover");
            foreach(var height in new[]{HeightLevel.Low,HeightLevel.Medium,HeightLevel.High})
            {
                var p=SessionSelection.BuildProfile(song,SessionMode.Therapy,height,FreeDifficulty.Easy);
                check(p.chart.All(n=>Math.Abs(n.TargetHeight-(height==HeightLevel.Low?ChartNote.LowTargetHeight:height==HeightLevel.Medium?ChartNote.MidTargetHeight:1))<.0001f),"Therapy height stays fixed");
            }
            check(original.SequenceEqual(song.therapyChart.notes.Select(n=>n.TargetHeight)),"Therapy choices do not mutate the saved chart");
        }
        return "PASS "+checks+" classical chart, difficulty, hold, full-length scoring, therapy and session-duration checks. No sessions or settings saved.";
    }
}
