using TMPro;
using UnityEngine;

namespace RhythmRehab
{
    // A silent, automatic illustration. It never starts a session or changes saved results.
    public sealed class ModeGameplayDemo : MonoBehaviour
    {
        [SerializeField] private TMP_Text title, explanation, feedback, score;
        [SerializeField] private UnityEngine.UI.Image accent;
        [SerializeField] private RectTransform marker;
        [SerializeField] private RectTransform[] heads, holdBodies;
        [SerializeField] private UnityEngine.UI.Image[] noteImages;
        private readonly float[] freeHeights = { 0, .25f, .68f, .5f, 1, .35f };
        private const float Interval = 1.65f, Travel = 2.5f, Left = -235, Right = 300, Height = 100;
        private SessionMode mode;
        private float elapsed, feedbackAge;
        private int lastHit, points;
        public float DemoTime => elapsed;
        public SessionMode Mode => mode;

        public void Play(SessionMode selected)
        {
            mode = selected; elapsed = 0; lastHit = -1; points = 0; feedbackAge = 2;
            title.text = selected == SessionMode.Therapy ? "THERAPY MODE · PREVIEW" : "FREE MODE · PREVIEW";
            explanation.text = selected == SessionMode.Therapy ?
                "One fixed height throughout.\nMID shown here — choose your height next." :
                "Follow notes at changing heights.\nNotes can also sit between the guide lines.";
            accent.color = selected == SessionMode.Therapy ? new Color(.68f,.32f,1) : new Color(.15f,.85f,1);
            score.text = "DEMO SCORE  0"; feedback.text = "";
            foreach (var head in heads) head.gameObject.SetActive(false);
            marker.anchoredPosition = new Vector2(Left, Target(0));
        }
        private float Target(int index) => Mathf.Lerp(-Height, Height, mode == SessionMode.Therapy ? .5f : freeHeights[index % freeHeights.Length]);
        private Color Tint(int index)
        {
            float h = Mathf.InverseLerp(-Height, Height, Target(index));
            return h <= .5f ? Color.Lerp(new Color(.3f,1,.45f),new Color(1,.75f,.15f),h*2) :
                Color.Lerp(new Color(1,.75f,.15f),new Color(1,.3f,.5f),(h-.5f)*2);
        }
        private void Update()
        {
            if (IrisSceneTransition.IsBusy) return;
            elapsed += Time.unscaledDeltaTime; feedbackAge += Time.unscaledDeltaTime;
            float cycle = heads.Length * Interval;
            for (int i = 0; i < heads.Length; i++)
            {
                float remaining = i * Interval + Travel - elapsed;
                if (remaining < -Travel) remaining = Mathf.Repeat(remaining + Travel, cycle) - Travel;
                bool hold = i == 3;
                bool holding = hold && remaining <= 0 && remaining > -1;
                bool visible = remaining <= Travel && (remaining > 0 || holding);
                heads[i].gameObject.SetActive(visible);
                if (!visible) continue;
                heads[i].anchoredPosition = new Vector2(holding ? Left : Mathf.Lerp(Left,Right,remaining/Travel),Target(i));
                noteImages[i].color = Tint(i);
                holdBodies[i].gameObject.SetActive(hold);
                if (hold)
                {
                    holdBodies[i].sizeDelta = new Vector2((holding ? 1 + remaining : 1) * (Right-Left)/Travel,7);
                    var image = holdBodies[i].GetComponent<UnityEngine.UI.Image>();
                    var tint = Tint(i); tint.a = .45f; image.color = tint;
                }
            }
            int hit = Mathf.FloorToInt((elapsed - Travel) / Interval);
            if (hit >= 0 && hit > lastHit)
            {
                lastHit = hit; points += 1000; feedbackAge = 0;
                feedback.text = hit % heads.Length == 3 ? "HOLD STEADY" : "PERFECT";
                score.text = $"DEMO SCORE  {points:N0}";
            }
            int current = hit >= 0 ? hit % heads.Length : 0;
            float afterHit = elapsed - Travel - Mathf.Max(0,hit) * Interval;
            float wait = current == 3 ? 1 : .15f;
            float blend = hit < 0 ? 0 : Mathf.SmoothStep(0,1,Mathf.InverseLerp(wait,Interval-.15f,afterHit));
            marker.anchoredPosition = new Vector2(Left,Mathf.Lerp(Target(current),Target((current+1)%heads.Length),blend));
            var colour = new Color(1,.85f,.35f,1-Mathf.Clamp01(feedbackAge/1.1f)); feedback.color = colour;
            feedback.rectTransform.anchoredPosition = new Vector2(35,40 + Mathf.Clamp01(feedbackAge)*22);
            score.rectTransform.localScale = Vector3.one * (1 + .08f * (1-Mathf.Clamp01(feedbackAge/.25f)));
        }
    }
}
