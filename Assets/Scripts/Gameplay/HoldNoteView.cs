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
        public void Render(float length,Color colour,bool holding,float progress,float songTime,bool reduced)
        {
            body.gameObject.SetActive(true);caption.gameObject.SetActive(true);
            body.rectTransform.sizeDelta=new Vector2(Mathf.Max(1,length-18),18);
            var tint=colour;tint.a=holding?.48f:.3f;body.color=tint;
            tint=holding?Color.Lerp(colour,Color.white,reduced?.24f:.55f):colour;
            if (holding)
            {
                float sweep = Mathf.Repeat(songTime * (reduced ? .4f : .75f), 1f);
                shine.rectTransform.anchorMin = new Vector2(Mathf.Clamp01(sweep - .12f), .5f);
                shine.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(sweep + .12f), .5f);
                shine.rectTransform.sizeDelta = new Vector2(0, reduced ? 3 : 5);
                tint.a = (reduced ? .55f : .94f) * Mathf.SmoothStep(0, 1, Mathf.Min(sweep, 1 - sweep) * 10);
            }
            else
            {
                shine.rectTransform.anchorMin = new Vector2(0, .5f);
                shine.rectTransform.anchorMax = new Vector2(1, .5f);
                shine.rectTransform.sizeDelta = new Vector2(-4, 3);
                tint.a = .85f;
            }
            shine.rectTransform.anchoredPosition = Vector2.zero;
            shine.color = tint;
            tail.color=colour;caption.text=holding?$"HOLD {Mathf.RoundToInt(progress*100)}%":"HOLD";
        }
        public void Hide(){body.gameObject.SetActive(false);caption.gameObject.SetActive(false);}
    }
}
