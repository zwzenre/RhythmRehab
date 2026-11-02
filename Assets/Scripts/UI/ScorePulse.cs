using UnityEngine;

namespace RhythmRehab
{
    [DisallowMultipleComponent]
    public sealed class ScorePulse : MonoBehaviour
    {
        [SerializeField, Range(1,1.5f)] private float peakScale = 1.24f;
        [SerializeField, Min(.05f)] private float duration = .3f;
        private Vector3 baseScale;
        private float elapsed, initialScale=1;
        private bool running;
        public int PulseCount { get; private set; }
        public float CurrentScale => baseScale.x==0?1:transform.localScale.x/baseScale.x;
        private void Awake()=>baseScale=transform.localScale;
        public void Punch()
        {
            if(!isActiveAndEnabled)return;
            initialScale=CurrentScale;elapsed=0;running=true;PulseCount++;
        }
        private void LateUpdate()
        {
            if(!running)return;
            elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/duration);
            float scale=t<.25f?Mathf.Lerp(initialScale,peakScale,1-Mathf.Pow(1-t/.25f,3)):
                Mathf.Lerp(1,peakScale,Mathf.Pow(1-(t-.25f)/.75f,3));
            transform.localScale=baseScale*scale;
            if(t>=1){running=false;transform.localScale=baseScale;}
        }
        private void OnDisable(){running=false;if(baseScale!=Vector3.zero)transform.localScale=baseScale;}
    }
}
