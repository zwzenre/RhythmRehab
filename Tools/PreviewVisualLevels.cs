using System;
using System.Reflection;
using UnityEngine;
using RhythmRehab;
using TMPro;
public static class PreviewVisualLevels
{
    public static string Main()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();
        if(!game||game.Summary==null||IrisSceneTransition.IsBusy)throw new Exception("Wait for Gameplay.");
        game.enabled=false;if(game.IsPaused)game.TogglePause();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(PrototypeGameplay);
        // Prevent focus events from opening the pause overlay during visual-only captures.
        GameplayEditorAccess.Write(game,"finished",true);
        GameplayEditorAccess.Write(game,"currentHeight",.5f);GameplayEditorAccess.Invoke(game,"UpdateMarker",null);
        var human=UnityEngine.Object.FindAnyObjectByType<HumanLegTracker>();human.enabled=false;human.ApplyHeight(.5f);
        var chart=(ChartNote[])GameplayEditorAccess.Read(game,"chart");
        var notes=(RectTransform[])GameplayEditorAccess.Read(game,"noteViews");
        for(int i=0;i<notes.Length;i++)
        {
            notes[i].gameObject.SetActive(i<6);
            if(i<6)notes[i].anchoredPosition=new Vector2(-350+i*230,Mathf.Lerp(-280,280,chart[i%chart.Length].TargetHeight));
        }
        ((JudgementTextFeedback)GameplayEditorAccess.Read(game,"judgementMotion")).SetIdle("",Color.white);
        return "Visual-only preview: gameplay disabled, representative note positions and mid-height seated tracker; no sessions saved.";
    }
}
