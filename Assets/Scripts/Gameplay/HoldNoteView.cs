using TMPro;
using UnityEngine;
namespace RhythmRehab
{
    // All children are authored in the scene, never created during play.
    public sealed class HoldNoteView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image body;
        [SerializeField] private UnityEngine.UI.Image shine;
        [SerializeField] private UnityEngine.UI.Image tail;
        [SerializeField] private TMP_Text caption;
        public void Render(float length,Color colour,bool holding,float progress)
        {
            body.gameObject.SetActive(true);caption.gameObject.SetActive(true);
            body.rectTransform.sizeDelta=new Vector2(Mathf.Max(1,length-18),18);
            var tint=colour;tint.a=holding?.48f:.3f;body.color=tint;
            tint=holding?Color.Lerp(colour,Color.white,.45f):colour;tint.a=.85f;shine.color=tint;
            tail.color=colour;caption.text=holding?$"HOLD {Mathf.RoundToInt(progress*100)}%":"HOLD";
        }
        public void Hide(){body.gameObject.SetActive(false);caption.gameObject.SetActive(false);}
    }
}
