using UnityEngine;

namespace RhythmRehab
{
    public sealed class VisualLevelHitAccent : MonoBehaviour
    {
        public enum Accent { SoftLight, Star, Shard }
        [SerializeField] private GameplayVisualLevels controller;
        [SerializeField] private UnityEngine.UI.Graphic graphic;
        [SerializeField] private Accent style;
        [SerializeField, Min(.1f)] private float duration = .75f;
        private Vector2 origin,direction;
        private Color tint;
        private float elapsed;
        private void Awake(){graphic.enabled=false;}
        public void Play(Vector2 position,Color colour,int index)
        {
            elapsed=0;origin=position;
            direction=index%3==0?new Vector2(.75f,.65f):index%3==1?new Vector2(.75f,-.65f):Vector2.right;
            tint=colour;tint.a=style==Accent.SoftLight?.24f:.65f;
            graphic.enabled=true;Draw();
        }
        private void Update()
        {
            if(!graphic.enabled||controller.Frozen)return;
            elapsed+=Time.unscaledDeltaTime;
            if(elapsed>=duration)graphic.enabled=false;else Draw();
        }
        private void Draw()
        {
            float t=Mathf.Clamp01(elapsed/duration);
            float distance=style==Accent.SoftLight?22:style==Accent.Star?105:155;
            graphic.rectTransform.anchoredPosition=origin+direction*distance*t;
            graphic.rectTransform.localScale=Vector3.one*(style==Accent.SoftLight?Mathf.Lerp(.3f,2.1f,t):Mathf.Lerp(1,.35f,t));
            graphic.rectTransform.localRotation=Quaternion.Euler(0,0,style==Accent.Star?t*150:style==Accent.Shard?-25:0);
            var c=tint;c.a*=1-Mathf.SmoothStep(0,1,t);graphic.color=c;
        }
        public void Clear(){if(graphic)graphic.enabled=false;}
    }
}
