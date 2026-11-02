using TMPro;
using UnityEngine;

namespace RhythmRehab
{
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class JudgementTextFeedback : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float duration = .7f;
        [SerializeField] private float rise = 58;
        [SerializeField] private NoteHitEffects textEffects;
        private TMP_Text label;
        private RectTransform rect;
        private Vector2 origin;
        private Vector3 baseScale;
        private Color displayColor;
        private float elapsed;
        private bool running;
        public bool IsAnimating => running;
        public float CurrentOpacity => label?label.color.a:0;
        private void Awake(){label=GetComponent<TMP_Text>();rect=label.rectTransform;origin=rect.anchoredPosition;baseScale=rect.localScale;}
        public void Show(string text,Color color,HitJudgement judgement)
        {
            if(!label)return;
            var entrance=GetComponent<TextSlideIn>();if(entrance)entrance.CompleteImmediately();
            elapsed=0;running=true;displayColor=color;displayColor.a=1;
            label.text=text;label.color=displayColor;rect.anchoredPosition=origin-Vector2.up*10;rect.localScale=baseScale*.92f;
            if(textEffects)
            {
                textEffects.Clear();
                if(judgement!=HitJudgement.Miss)
                {
                    float side=Mathf.Min(250,label.GetPreferredValues(text).x*.5f+26);
                    textEffects.Play(new Vector2(-side,0),judgement);
                    textEffects.Play(new Vector2(side,0),judgement);
                }
            }
        }
        public void SetIdle(string text,Color color)
        {
            running=false;Restore();if(textEffects)textEffects.Clear();
            label.text=text;label.color=color;
        }
        private void LateUpdate()
        {
            if(!running)return;
            elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/duration);
            float ease=1-Mathf.Pow(1-t,3);
            rect.anchoredPosition=origin+Vector2.up*Mathf.Lerp(-10,rise,ease);
            rect.localScale=baseScale*Mathf.Lerp(.92f,1.06f,Mathf.Clamp01(t/.15f));
            var tint=displayColor;tint.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,1,t));label.color=tint;
            if(t>=1){running=false;label.text="";Restore();}
        }
        private void Restore(){if(rect){rect.anchoredPosition=origin;rect.localScale=baseScale;}}
        private void OnDisable(){running=false;Restore();if(textEffects)textEffects.Clear();}
    }
}
