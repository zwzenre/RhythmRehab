using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class RhythmCalibrationController : MonoBehaviour
    {
        [Header("Saved preview objects")]
        [SerializeField] private RectTransform playfield, marker;
        [SerializeField] private RectTransform[] notes;
        [SerializeField] private Image[] noteImages;
        [SerializeField] private Slider offsetSlider, speedSlider;
        [SerializeField] private TMP_Text offsetValue, speedValue, feedback, statistics, saveStatus, startLabel;
        [SerializeField] private AudioSource metronome;
        [SerializeField] private Slider hitSoundTiming;
        [SerializeField] private TMP_Text hitSoundTimingValue;
        [SerializeField] private AudioSource soundPreview;
        [SerializeField] private NoteHitEffects hitEffects;
        [SerializeField] private JudgementTextFeedback judgementMotion;
        [SerializeField] private UIScreenAudio screenAudio;
        private AudioClip previewClip, originalPreviewClip;
        [SerializeField] private float judgementX = -350, spawnX = 450, laneSpacing = 110;
        // Repeat only the three guide heights so timing practice stays simple.
        private readonly float[] heights = { 0, .5f, 1 };
        private double[] targets;
        private bool[] resolved;
        private AudioClip click;
        private double startDsp;
        private bool playing;
        private int hits, misses, attempts;
        private double totalErrorMs;
        private float feedbackUntil;
        public bool IsPlaying => playing;
        public float OffsetMs => offsetSlider.value;
        public float NoteSpeed => speedSlider.value;
        public float Elapsed => (float)(AudioSettings.dspTime - startDsp);

        private void Start()
        {
            offsetSlider.SetValueWithoutNotify(RhythmPreferences.OffsetMs);
            speedSlider.SetValueWithoutNotify(RhythmPreferences.Speed);
            foreach(var n in notes)n.gameObject.SetActive(false);
            targets = new double[notes.Length];resolved = new bool[notes.Length];
            click = MakeClick();metronome.clip = click;metronome.loop = true;
            var settings = LocalSettingsStore.Default.Load(out var loadError);
            metronome.volume = settings.beatVolume;
            hitSoundTiming.SetValueWithoutNotify(settings.hitSoundOffsetMs);
            originalPreviewClip = soundPreview.clip;
            soundPreview.volume = settings.sfxVolume;
            RebuildHitSound();
            UpdateLabels();feedback.text = "PRESS START TO TRY THE RHYTHM";
            if(judgementMotion)judgementMotion.SetIdle(feedback.text,Color.white);
            statistics.text = "No practice hits yet.\nThis preview is not saved to session history.";
            saveStatus.text = loadError == null ? "Saved values loaded. Adjust, test, then SAVE." : "Could not load settings: " + loadError;
        }
        private static AudioClip MakeClick()
        {
            const int rate=22050;var samples=new float[rate/2];
            for(int i=0;i<1100;i++)samples[i]=Mathf.Sin(2*Mathf.PI*1100*i/rate)*Mathf.Exp(-i/170f)*.35f;
            var result=AudioClip.Create("120 BPM calibration beat",samples.Length,1,rate,false);result.SetData(samples,0);return result;
        }
        public void Adjust(float value)
        { UpdateLabels();saveStatus.text="UNSAVED CHANGES · Preview updates immediately. SAVE applies to gameplay."; }
        private void UpdateLabels()
        { offsetValue.text=$"{OffsetMs:+0;-0;0} ms";speedValue.text=$"{NoteSpeed:0.00}x";hitSoundTimingValue.text=$"{hitSoundTiming.value:+0;-0;0} ms"; }
        public void AdjustHitSound(float value) { RebuildHitSound(); Adjust(value); }
        private void RebuildHitSound()
        {
            soundPreview.Stop();
            if(previewClip && previewClip != originalPreviewClip)Destroy(previewClip);
            previewClip=HitSoundTiming.Adjust(originalPreviewClip,hitSoundTiming.value);
        }
        public void TestHitSound() { if(previewClip)soundPreview.PlayOneShot(previewClip);ClearSelection(); }
        public void ResetValues()
        {offsetSlider.SetValueWithoutNotify(0);speedSlider.SetValueWithoutNotify(1);hitSoundTiming.SetValueWithoutNotify(0);AdjustHitSound(0);}
        public void Save()
        {
            saveStatus.text = LocalSettingsStore.Default.TrySaveCalibration(OffsetMs,NoteSpeed,hitSoundTiming.value,out var error)
                ? "SAVED · Judgement timing, note speed and hit sound apply to gameplay."
                : "NOT SAVED · " + error;
            ClearSelection();
        }
        private static void ClearSelection(){if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);}
        public void TogglePreview()
        {
            ClearSelection();if(playing){StopPreview();return;}
            if(IrisSceneTransition.IsBusy||targets==null)return;
            if(hitEffects)hitEffects.Clear();
            if(screenAudio)screenAudio.SetPracticeActive(true);
            hits=misses=attempts=0;totalErrorMs=0;
            for(int i=0;i<targets.Length;i++){targets[i]=2+i;resolved[i]=false;noteImages[i].color=HeightColor(heights[i%heights.Length]);}
            startDsp=AudioSettings.dspTime+.2;metronome.PlayScheduled(startDsp);playing=true;startLabel.text="STOP PREVIEW";
            feedback.text="GET READY · 120 BPM";feedbackUntil=Time.unscaledTime+2;
            if(judgementMotion)judgementMotion.SetIdle(feedback.text,Color.white);
            UpdateStats();
        }
        private void Update()
        {
            if(!playing||IrisSceneTransition.IsBusy)return;
            float height=0;
            if(Mouse.current!=null&&RectTransformUtility.ScreenPointToLocalPointInRectangle(playfield,Mouse.current.position.ReadValue(),null,out var local))height=Mathf.InverseLerp(-laneSpacing,laneSpacing,local.y);
            marker.anchoredPosition=new Vector2(judgementX,Mathf.Lerp(-laneSpacing,laneSpacing,height));marker.GetComponent<Image>().color=Color.white;
            double time=Elapsed;float travel=RhythmPreferences.TravelSeconds(3,NoteSpeed),offset=OffsetMs/1000;
            for(int i=0;i<notes.Length;i++)
            {
                double remaining=targets[i]-time;
                if(!resolved[i]&&remaining+offset < -RhythmRules.GoodWindow){resolved[i]=true;misses++;UpdateStats();}
                if(time>targets[i]+.8){targets[i]+=notes.Length;resolved[i]=false;remaining=targets[i]-time;}
                bool visible=!resolved[i]&&remaining<=travel&&remaining+offset>=-RhythmRules.GoodWindow;
                notes[i].gameObject.SetActive(visible);
                if(visible)notes[i].anchoredPosition=new Vector2(Mathf.LerpUnclamped(judgementX,spawnX,(float)remaining/travel),Mathf.Lerp(-laneSpacing,laneSpacing,heights[i%heights.Length]));
            }
            if(Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)TryHit(time,height);
            if(Time.unscaledTime>feedbackUntil)
            {
                if(judgementMotion&&!judgementMotion.IsAnimating)judgementMotion.SetIdle("MOUSE UP / DOWN · SPACE TO HIT",Color.white);
                else if(!judgementMotion)feedback.text="MOUSE UP / DOWN · SPACE TO HIT";
            }
        }
        public bool TryHit(double time,float height)
        {
            if(!playing)return false;int candidate=-1;double closest=RhythmRules.GoodWindow+.000001;
            for(int i=0;i<targets.Length;i++)
            {
                double error=Math.Abs(RhythmPreferences.TimingError(time,targets[i],OffsetMs/1000));
                if(!resolved[i]&&error<=closest){candidate=i;closest=error;}
            }
            if(candidate<0){feedback.text="TRY THE NEXT BEAT";if(judgementMotion)judgementMotion.SetIdle(feedback.text,new Color(.65f,.74f,.88f));feedbackUntil=Time.unscaledTime+1;return false;}
            double timing=RhythmPreferences.TimingError(time,targets[candidate],OffsetMs/1000);
            float heightError=Mathf.Abs(height-heights[candidate%heights.Length]);
            var judgement=RhythmRules.Judge(timing,heightError,.14f);resolved[candidate]=true;notes[candidate].gameObject.SetActive(false);
            if(judgement==HitJudgement.Miss)misses++;
            else
            {
                hits++;attempts++;totalErrorMs+=(time-targets[candidate])*1000;
                if(previewClip)soundPreview.PlayOneShot(previewClip);
                if(hitEffects)hitEffects.Play(new Vector2(judgementX,Mathf.Lerp(-laneSpacing,laneSpacing,heights[candidate%heights.Length])),judgement);
            }
            feedback.text=judgement==HitJudgement.Miss?(heightError>.14f?"ADJUST YOUR HEIGHT":"KEEP GOING"):$"{judgement.ToString().ToUpperInvariant()} · {timing*1000:+0;-0;0} ms";
            feedback.color=judgement==HitJudgement.Miss?new Color(.65f,.74f,.88f):hitEffects?hitEffects.ColorFor(judgement):Color.white;
            if(judgementMotion)judgementMotion.Show(feedback.text,feedback.color,judgement);
            feedbackUntil=Time.unscaledTime+1;UpdateStats();return true;
        }
        private void UpdateStats()
        {statistics.text=$"HITS {hits}  ·  TO PRACTISE {misses}\n"+(attempts>0?$"Average raw timing: {totalErrorMs/attempts:+0;-0;0} ms":"Match height and press Space on the beat.");}
        private static Color HeightColor(float h)=>h<=.5f?Color.Lerp(new Color(.3f,1,.45f),new Color(1,.75f,.15f),h*2):Color.Lerp(new Color(1,.75f,.15f),new Color(.65f,.74f,.88f),(h-.5f)*2);
        public void StopPreview()
        {playing=false;metronome.Stop();soundPreview.Stop();if(hitEffects)hitEffects.Clear();if(screenAudio)screenAudio.SetPracticeActive(false);foreach(var n in notes)n.gameObject.SetActive(false);startLabel.text="START PREVIEW";feedback.text="PREVIEW STOPPED";feedback.color=Color.white;if(judgementMotion)judgementMotion.SetIdle(feedback.text,Color.white);}
        public void Back(){StopPreview();IrisSceneTransition.Load("Settings");}
        public void Home(){StopPreview();IrisSceneTransition.Load("MainMenu");}
        private void OnApplicationFocus(bool focused){if(!focused&&playing)StopPreview();}
        private void OnDestroy(){if(click!=null)Destroy(click);if(previewClip&&previewClip!=originalPreviewClip)Destroy(previewClip);}
    }
}
