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
        private IncomingNoteSparks sparks;

        private void Awake() => sparks = GetComponentInChildren<IncomingNoteSparks>(true);

        public void Render(float length, Color colour, bool holding, float progress, bool reducedEffects)
        {
            body.gameObject.SetActive(true);
            caption.gameObject.SetActive(true);
            body.rectTransform.sizeDelta = new Vector2(Mathf.Max(1, length - 18), 18);
            float glow = holding ? Mathf.Clamp01(progress) : 0;
            Color tint = Color.Lerp(colour, Color.white, glow);
            tail.color = tint;
            tint.a = holding ? Mathf.Lerp(.48f, 1f, glow) : .3f;
            body.color = tint;
            if (shine)
            {
                shine.enabled = !holding;
                if (!holding) shine.color = colour;
            }
            caption.text = holding ? $"HOLD {Mathf.Min(99, Mathf.RoundToInt(progress * 100))}%" : "HOLD";
            if (sparks) sparks.SetHoldProgress(holding ? progress : -1, reducedEffects);
        }

        public void ShowCompletion(bool reducedEffects)
        {
            body.gameObject.SetActive(false);
            caption.gameObject.SetActive(true);
            caption.text = "HOLD 100%";
            if (sparks) sparks.SetHoldProgress(1, reducedEffects);
        }

        public void Hide()
        {
            body.gameObject.SetActive(false);
            caption.gameObject.SetActive(false);
            if (sparks) sparks.SetHoldProgress(-1, false);
        }
    }
}
