using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using RhythmRehab;

public static class NoteHitRipplesProbe
{
    static string status, stage;
    static float began, since, diameter, alpha;
    static int checks;
    static NoteHitEffects effects;
    static NoteHitRipples ripples;
    static UnityEngine.UI.RawImage[] rings;
    static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static object Field(object target, string name) => target.GetType().GetField(name, flags).GetValue(target);
    static void Check(bool pass, string message) { if (!pass) throw new Exception(message); checks++; }
    static void Stage(string value) { stage = value; since = Time.realtimeSinceStartup; }
    public static string Main()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Enter Play mode first.");
        checks = 0; status = "RUNNING"; began = Time.realtimeSinceStartup;
        SessionWellbeing.Reset(); IrisSceneTransition.Load("Gameplay"); Stage("game");
        EditorApplication.update += Tick; return "Testing successful hits, colours, motion, pause, misses and calibration.";
    }
    static void Tick()
    {
        try
        {
            if (Time.realtimeSinceStartup - began > 35) throw new Exception("Timeout: " + stage);
            if (IrisSceneTransition.IsBusy) return;
            float elapsed = Time.realtimeSinceStartup - since;
            if (stage == "game")
            {
                if (SceneManager.GetActiveScene().name != "Gameplay") return;
                var game = UnityEngine.Object.FindAnyObjectByType<PrototypeGameplay>();
                if (game.Summary == null) return;
                game.enabled = false;
                effects = (NoteHitEffects)Field(game,"hitEffects");
                ripples = (NoteHitRipples)Field(effects,"ripples");
                rings = (UnityEngine.UI.RawImage[])Field(ripples,"rings");
                Check(rings.Length == 8 && rings.All(r=>r && r.texture && !r.raycastTarget), "Eight authored, textured, nonblocking rings");
                Check(((NoteHitRipples)Field((NoteHitEffects)Field((JudgementTextFeedback)Field(game,"judgementMotion"),"textEffects"),"ripples")) == null, "Text sparkles do not duplicate note ripples");
                effects.Clear();
                var chart = (ChartNote[])Field(game,"chart"); Array.Clear((bool[])Field(game,"resolved"),0,chart.Length);
                float offset = (float)Field(game,"judgementOffsetSeconds");
                Check(game.TryHit(chart[0].beat*60.0/game.Summary.bpm+offset,chart[0].TargetHeight), "Successful note consumed");
                Check(ripples.PlayedRipples == 1 && rings[0].enabled, "Successful gameplay hit emits ripple");
                Check(rings[0].color.r > .9f && rings[0].color.g > .7f && rings[0].color.b < .3f, "Perfect ring is yellow");
                diameter = rings[0].rectTransform.sizeDelta.x; alpha = rings[0].color.a;
                int before = ripples.PlayedRipples;
                game.TryHit(chart[1].beat*60.0/game.Summary.bpm+offset,chart[1].TargetHeight>.5f?0:1);
                Check(ripples.PlayedRipples == before, "Wrong height miss emits no ripple"); Stage("expand");
            }
            else if(stage == "expand" && elapsed > .15f)
            {
                Check(rings[0].enabled && rings[0].rectTransform.sizeDelta.x > diameter+10, "Ring expands over live frames");
                Check(rings[0].color.a < alpha, "Ring fades over live frames");
                effects.SetPaused(true); diameter = rings[0].rectTransform.sizeDelta.x; alpha = rings[0].color.a; Stage("pause");
            }
            else if(stage == "pause" && elapsed > .2f)
            {
                Check(rings[0].rectTransform.sizeDelta.x == diameter && rings[0].color.a == alpha, "Pause freezes ripple");
                effects.SetPaused(false); Stage("expire");
            }
            else if(stage == "expire" && elapsed > 1.3f)
            {
                Check(rings.All(r=>!r.enabled), "Rings disappear after lifetime");
                effects.Clear();effects.Play(Vector2.zero,HitJudgement.Perfect);
                ((float[])Field(ripples,"ages"))[0]=(float)Field(ripples,"duration")*.6f;
                typeof(NoteHitRipples).GetMethod("Draw",flags).Invoke(ripples,new object[]{0});
                var viewport=(RectTransform)Field(ripples,"viewport");
                Check(rings[0].rectTransform.sizeDelta.x*.5f>=viewport.rect.width*.5f,"Wave reaches at least half the screen width toward the right before expiring");
                Check(rings[0].color.a>0 && rings[0].color.a<.15f,"Large wave remains visible at low opacity");
                Check(ripples.transform.GetSiblingIndex()==0 && ripples.GetComponent<UnityEngine.UI.RectMask2D>(),"Ripple stays behind notes and clips to playfield");
                effects.Clear(); effects.Play(new Vector2(0,0),HitJudgement.Great); effects.Play(new Vector2(30,80),HitJudgement.Good);
                Check(rings[0].enabled && rings[1].enabled, "Consecutive hits overlap independently");
                Check(rings[0].color.r>.6f && rings[0].color.b>.9f && rings[0].color.g<.4f, "Great ring is purple");
                Check(rings[1].color.b>.9f && rings[1].color.r<.3f, "Good ring is blue");
                Check(rings[1].rectTransform.anchoredPosition==new Vector2(30,80), "Ring follows supplied note position");
                for(int i=0;i<12;i++) effects.Play(new Vector2(i,0),HitJudgement.Perfect);
                Check(rings.Count(r=>r.enabled)==8 && ripples.transform.childCount==8, "Burst pool reuses authored rings without creating objects");
                effects.Clear(); Check(rings.All(r=>!r.enabled), "Clear hides all ripples");
                IrisSceneTransition.Load("RhythmCalibration"); Stage("calibration");
            }
            else if(stage == "calibration")
            {
                if(SceneManager.GetActiveScene().name!="RhythmCalibration")return;
                var calibration=UnityEngine.Object.FindAnyObjectByType<RhythmCalibrationController>();
                calibration.enabled=false;calibration.ResetValues();calibration.TogglePreview();
                effects=(NoteHitEffects)Field(calibration,"hitEffects");ripples=(NoteHitRipples)Field(effects,"ripples");
                rings=(UnityEngine.UI.RawImage[])Field(ripples,"rings");
                Check(calibration.TryHit(2,0) && ripples.PlayedRipples==1 && rings.Any(r=>r.enabled), "Calibration successful hit emits ripple");
                calibration.StopPreview();Check(rings.All(r=>!r.enabled),"Stopping calibration clears rings");
                status="PASS "+checks+" live ripple checks. No settings or sessions saved.";EditorApplication.update-=Tick;
            }
        }
        catch(Exception error){status="FAIL "+error.Message;EditorApplication.update-=Tick;}
    }
    public static string Status()=>status;
}
