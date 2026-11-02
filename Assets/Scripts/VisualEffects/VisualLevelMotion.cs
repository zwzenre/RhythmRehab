using UnityEngine;

namespace RhythmRehab
{
    public sealed class VisualLevelMotion : MonoBehaviour
    {
        public enum Motion { Float, Rotate, Drift }
        [SerializeField] private GameplayVisualLevels controller;
        [SerializeField] private Motion motion;
        [SerializeField] private Vector2 amplitude = new Vector2(30,12);
        [SerializeField] private float speed = .2f;
        [SerializeField] private float phase;
        private RectTransform rect;
        private Vector2 origin;
        private Quaternion rotation;
        private void Awake(){rect=GetComponent<RectTransform>();origin=rect.anchoredPosition;rotation=rect.localRotation;}
        private void LateUpdate()
        {
            float t=controller.Clock;
            if(motion==Motion.Rotate)rect.localRotation=rotation*Quaternion.Euler(0,0,t*speed);
            else if(motion==Motion.Drift)rect.anchoredPosition=new Vector2(-530+Mathf.Repeat(origin.x+530-t*speed,1480),origin.y+Mathf.Sin(t*.4f+phase)*amplitude.y);
            else rect.anchoredPosition=origin+new Vector2(Mathf.Sin(t*speed+phase)*amplitude.x,Mathf.Sin(t*speed*.73f+phase)*amplitude.y);
        }
    }
}
