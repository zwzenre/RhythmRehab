using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    [DisallowMultipleComponent]
    public sealed class GentleBackgroundMotion : MonoBehaviour
    {
        public enum MotionType { TextureDrift, Float }
        [SerializeField] private MotionType motionType;
        [Tooltip("Texture drift uses UV fractions. Floating decorations use Canvas pixels.")]
        [SerializeField] private Vector2 amplitude = new Vector2(12, 6);
        [SerializeField, Min(1)] private float period = 28;
        [SerializeField] private float phase;
        [SerializeField, Range(.9f,1)] private float textureWindow = .97f;
        [SerializeField, Range(0,15)] private float rotationSway;
        [SerializeField, Range(0,.08f)] private float scaleSway;
        private RectTransform rect;
        private RawImage image;
        private Vector2 origin;
        private Rect originalUV;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private float elapsed;
        private bool initialized;

        private void Awake()
        {
            rect=GetComponent<RectTransform>();image=GetComponent<RawImage>();
            if(rect)origin=rect.anchoredPosition;if(image)originalUV=image.uvRect;
            originalRotation=transform.localRotation;originalScale=transform.localScale;
            initialized=true;
        }
        private void OnEnable(){elapsed=0;if(initialized)Apply(0);}
        private void LateUpdate(){elapsed+=Time.unscaledDeltaTime;Apply(elapsed);}
        private void Apply(float time)
        {
            float angle=time*Mathf.PI*2/Mathf.Max(1,period);
            float easeIn=Mathf.SmoothStep(0,1,time/2);
            Vector2 offset=new Vector2(Mathf.Sin(angle+phase)*amplitude.x,Mathf.Sin(angle*.73f+phase*.83f)*amplitude.y)*easeIn;
            if(motionType==MotionType.TextureDrift&&image)
            {
                // Crop a small margin, so drifting never reveals an empty screen edge.
                float margin=(1-textureWindow)*.5f;
                offset.x=Mathf.Clamp(offset.x,-margin,margin);offset.y=Mathf.Clamp(offset.y,-margin,margin);
                image.uvRect=new Rect(originalUV.x+originalUV.width*(margin+offset.x),
                    originalUV.y+originalUV.height*(margin+offset.y),originalUV.width*textureWindow,originalUV.height*textureWindow);
            }
            else if(rect)
            {
                rect.anchoredPosition=origin+offset;
                float sway=Mathf.Sin(angle*.65f+phase)*easeIn;
                rect.localRotation=originalRotation*Quaternion.Euler(0,0,sway*rotationSway);
                rect.localScale=originalScale*(1+sway*scaleSway);
            }
        }
        private void OnDisable()
        {
            if(!initialized)return;
            if(motionType==MotionType.TextureDrift&&image)image.uvRect=originalUV;
            else if(rect){rect.anchoredPosition=origin;rect.localRotation=originalRotation;rect.localScale=originalScale;}
        }
    }
}
