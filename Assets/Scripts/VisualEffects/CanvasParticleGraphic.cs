using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace RhythmRehab
{
    // Draws the package's simulated particles through uGUI, above the overlay playfield.
    // The ParticleSystem and this Graphic are authored in the scene, never spawned at runtime.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CanvasParticleGraphic : MaskableGraphic
    {
        [SerializeField] private ParticleSystem source;
        [SerializeField] private Sprite particleSprite;
        [SerializeField] private float pixelsPerUnit = 35;
        private ParticleSystem.Particle[] particles;
        public override Texture mainTexture => particleSprite ? particleSprite.texture : Texture2D.whiteTexture;

        protected override void Awake()
        {
            base.Awake();
            if (source) particles = new ParticleSystem.Particle[source.main.maxParticles];
        }
        private void LateUpdate() { SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!source || particles == null) return;
            int count = source.GetParticles(particles);
            Vector4 uv = particleSprite ? DataUtility.GetOuterUV(particleSprite) : new Vector4(0, 0, 1, 1);
            for (int i = 0; i < count; i++)
            {
                var p = particles[i];
                Vector2 center = (Vector2)p.position * pixelsPerUnit;
                Vector3 size = p.GetCurrentSize3D(source) * pixelsPerUnit;
                float angle = p.rotation * Mathf.Deg2Rad;
                Vector2 right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size.x * .5f;
                Vector2 up = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * size.y * .5f;
                // The package is yellow; replace its RGB so purple/blue stay true to the judgement.
                Color tint = new Color(color.r, color.g, color.b, p.GetCurrentColor(source).a * color.a);
                int first = vh.currentVertCount;
                vh.AddVert(center - right - up, tint, new Vector2(uv.x, uv.y));
                vh.AddVert(center - right + up, tint, new Vector2(uv.x, uv.w));
                vh.AddVert(center + right + up, tint, new Vector2(uv.z, uv.w));
                vh.AddVert(center + right - up, tint, new Vector2(uv.z, uv.y));
                vh.AddTriangle(first, first + 1, first + 2);
                vh.AddTriangle(first + 2, first + 3, first);
            }
        }
    }
}
