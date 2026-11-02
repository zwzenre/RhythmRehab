using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RhythmRehab
{
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class UIHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField, Range(1,1.15f)] private float hoverScale = 1.06f;
        [SerializeField, Min(.01f)] private float duration = .15f;
        [SerializeField] private Color highlightColor = Color.white;
        [SerializeField, Range(0,1)] private float colourStrength = .2f;
        private Button button;
        private RectTransform rect;
        private Graphic graphic;
        private Vector3 baseScale;
        private Color baseColor, lastApplied;
        private bool pointerInside, keyboardSelected, applied;
        private float blend;
        public float CurrentScale => Mathf.Lerp(1,hoverScale,Mathf.SmoothStep(0,1,blend));

        private void Awake()
        {
            button=GetComponent<Button>();rect=GetComponent<RectTransform>();graphic=button.targetGraphic;
            baseScale=rect.localScale;if(graphic)baseColor=graphic.color;
        }
        public void OnPointerEnter(PointerEventData data){pointerInside=true;keyboardSelected=false;}
        public void OnPointerExit(PointerEventData data)=>pointerInside=false;
        public void OnSelect(BaseEventData data)=>keyboardSelected=!(data is PointerEventData);
        public void OnDeselect(BaseEventData data)=>keyboardSelected=false;
        private void LateUpdate()
        {
            // Keep colours chosen by page controllers (for example the active history tab).
            if(graphic&&(!applied||graphic.color!=lastApplied))baseColor=graphic.color;
            bool hover=(pointerInside||keyboardSelected)&&button.IsActive()&&button.IsInteractable()&&!IrisSceneTransition.IsBusy;
            if(blend==0&&!hover)return;
            blend=Mathf.MoveTowards(blend,hover?1:0,Time.unscaledDeltaTime/Mathf.Max(.01f,duration));
            float ease=Mathf.SmoothStep(0,1,blend);
            rect.localScale=baseScale*Mathf.Lerp(1,hoverScale,ease);
            if(graphic){lastApplied=Color.Lerp(baseColor,highlightColor,ease*colourStrength);graphic.color=lastApplied;applied=true;}
        }
        private void OnDisable()
        {
            pointerInside=keyboardSelected=false;blend=0;
            if(rect)rect.localScale=baseScale;
            if(graphic&&applied&&graphic.color==lastApplied)graphic.color=baseColor;
            applied=false;
        }
    }
}
