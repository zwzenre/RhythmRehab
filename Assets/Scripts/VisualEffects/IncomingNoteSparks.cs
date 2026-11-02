using UnityEngine;
using UnityEngine.Sprites;

namespace RhythmRehab
{
    // A small, reusable UI mesh on each authored note; no runtime objects or materials.
    public sealed class IncomingNoteSparks : UnityEngine.UI.MaskableGraphic
    {
        [SerializeField] private PrototypeGameplay gameplay;
        [SerializeField] private GameplayPlayfield playfield;
        [SerializeField] private UnityEngine.UI.Image noteImage;
        [SerializeField] private Sprite sparkle;
        [SerializeField, Range(3, 24)] private int sparkCount = 14;
        [SerializeField, Min(1)] private float sparkSize = 28;
        [SerializeField, Min(20)] private float trailLength = 195;
        [SerializeField, Min(1)] private float spread = 26;
        [SerializeField, Min(.1f)] private float lifetime = .65f;
        [SerializeField, Range(0, 1)] private float opacity = .85f;
        [SerializeField] private float phase;
        private float animationTime;
        private bool emitting;
        public override Texture mainTexture => sparkle ? sparkle.texture : Texture2D.whiteTexture;

        protected override void OnEnable() { base.OnEnable(); emitting = false; }
        private void LateUpdate()
        {
            if (!gameplay || gameplay.Notes == null || !playfield || !noteImage) return;
            if (gameplay.IsPaused || IrisSceneTransition.IsBusy) return;
            animationTime = gameplay.Elapsed;
            emitting = noteImage.rectTransform.anchoredPosition.x > playfield.JudgementX + 1;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            if (!emitting || !noteImage) return;
            Vector4 uv = sparkle ? DataUtility.GetOuterUV(sparkle) : new Vector4(0, 0, 1, 1);
            Color tint = Color.Lerp(noteImage.color, Color.white, .35f);
            for (int i = 0; i < sparkCount; i++)
            {
                float age = Mathf.Repeat(animationTime / Mathf.Max(.1f, lifetime) + phase + i * .618034f, 1);
                float seed = Mathf.Sin(i * 73.17f + phase * 11);
                Vector2 center = new Vector2(22 + age * trailLength, seed * spread * (.3f + age));
                float size = Mathf.Lerp(sparkSize, sparkSize * .22f, age) * (i % 3 == 0 ? 1.2f : .75f);
                float angle = (i * 41 + age * 65) * Mathf.Deg2Rad;
                Vector2 right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size * .5f;
                Vector2 up = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * size * .5f;
                tint.a = opacity * Mathf.Pow(1 - age, 1.1f) * Mathf.Clamp01(age * 12);
                int first = vh.currentVertCount;
                vh.AddVert(center - right - up, tint, new Vector2(uv.x, uv.y));
                vh.AddVert(center - right + up, tint, new Vector2(uv.x, uv.w));
                vh.AddVert(center + right + up, tint, new Vector2(uv.z, uv.w));
                vh.AddVert(center + right - up, tint, new Vector2(uv.z, uv.y));
                vh.AddTriangle(first, first + 1, first + 2); vh.AddTriangle(first + 2, first + 3, first);
            }
        }
    }
}
