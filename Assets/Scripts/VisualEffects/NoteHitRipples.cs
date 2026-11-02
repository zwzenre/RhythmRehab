using UnityEngine;

namespace RhythmRehab
{
    // Animates rings authored in the scene; no objects are created during play.
    public sealed class NoteHitRipples : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.RawImage[] rings;
        [SerializeField, Min(.1f)] private float duration = .6f;
        [SerializeField, Min(1f)] private float startDiameter = 24f;
        [SerializeField, Min(1f)] private float endDiameter = 170f;
        [SerializeField, Range(0f, 1f)] private float opacity = .85f;
        [Header("Screen coverage")]
        [SerializeField] private RectTransform viewport;
        [SerializeField, Range(0f, 1f), Tooltip("Minimum final radius as a fraction of screen width.")]
        private float minimumRightwardReach = .7f;
        private float[] ages;
        private Color[] colours;
        private int next;
        private bool paused;
        public int PlayedRipples { get; private set; }

        private void Awake()
        {
            ages = new float[rings.Length];
            colours = new Color[rings.Length];
            Clear();
        }

        public void Play(Vector2 position, HitJudgement judgement, Color colour)
        {
            if (judgement == HitJudgement.Miss || rings == null || rings.Length == 0) return;
            int slot = next++ % rings.Length;
            ages[slot] = 0;
            colours[slot] = colour;
            rings[slot].rectTransform.anchoredPosition = position;
            rings[slot].enabled = true;
            Draw(slot);
            PlayedRipples++;
        }

        private void Update()
        {
            if (paused) return;
            for (int i = 0; i < rings.Length; i++)
            {
                if (!rings[i].enabled) continue;
                ages[i] += Time.unscaledDeltaTime;
                if (ages[i] >= duration) rings[i].enabled = false;
                else Draw(i);
            }
        }

        private void Draw(int i)
        {
            float t = Mathf.Clamp01(ages[i] / Mathf.Max(.1f, duration));
            float eased = 1f - (1f - t) * (1f - t);
            // Radius, rather than diameter, determines how far the wave travels right of the hit.
            float diameter = viewport ? Mathf.Max(endDiameter, viewport.rect.width * minimumRightwardReach * 2f) : endDiameter;
            rings[i].rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(startDiameter, diameter, eased);
            Color colour = colours[i];
            colour.a *= opacity * (1f - Mathf.SmoothStep(0, 1, t));
            rings[i].color = colour;
        }

        public void SetPaused(bool value) => paused = value;

        public void Clear()
        {
            if (rings != null) foreach (var ring in rings) if (ring) ring.enabled = false;
            next = 0;
            paused = false;
        }

        private void OnDisable() => Clear();
    }
}
