using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    // Draw JMO's flare and spark textures in one mesh on the gameplay Canvas.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ChartAwardParticles : MaskableGraphic
    {
        [SerializeField] private Texture2D flare;
        private float elapsed;
        private Color tint;
        public override Texture mainTexture => flare ? flare : Texture2D.blackTexture;

        internal void ShowFrame(float time, Color colour)
        {
            elapsed = time;
            tint = colour;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float width = rectTransform.rect.width;
            float scale = Mathf.Clamp(width / 1920f, .6f, 1.35f);

            for (int i = 0; i < 82; i++)
            {
                float delay = Noise(i, 1) * .30f;
                float age = (elapsed - delay) / (.27f + Noise(i, 2) * .27f);
                if (age < 0 || age >= 1) continue;
                float x = Mathf.Lerp(width * .55f, -width * .42f, age);
                float y = Signed(i, 3) * (35 + Noise(i, 4) * 155) * scale;
                float length = (18 + Noise(i, 5) * 100) * scale * (1 - age * .65f);
                Spark(mesh, new Vector2(x, y), Vector2.right, length, (4 + Noise(i, 6) * 12) * scale,
                    Color.Lerp(tint, Color.white, Noise(i, 7)), Mathf.Sin(age * Mathf.PI) * .65f);
            }

            for (int i = 0; i < 190; i++)
            {
                float age = (elapsed - .27f - Noise(i, 8) * .38f) / (.42f + Noise(i, 9) * 1.02f);
                if (age < 0 || age >= 1) continue;
                float angle = Noise(i, 10) * Mathf.PI * 2;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float travel = 1 - (1 - age) * (1 - age);
                float radius = 60 + Noise(i, 11) * 570;
                var origin = new Vector2(Signed(i, 12) * 285, Signed(i, 13) * 28);
                var point = (origin + Vector2.Scale(direction, new Vector2(radius, radius * .54f)) * travel) * scale;
                float glint = Mathf.SmoothStep(0, 1, .5f + .5f * Mathf.Cos(elapsed * 16 + i));
                float alpha = Mathf.Min(1, age * 13) * (1 - age) * (.36f + .64f * glint);
                float size = (4 + Noise(i, 14) * 14) * scale * Mathf.Lerp(1f, .6f, age);
                var colour = Color.Lerp(tint, Color.white, Noise(i, 15) * .88f);
                bool streak = i % 5 == 0;
                Spark(mesh, point, direction, size * (streak ? 3.25f : 1f),
                    size * (streak ? .62f : 1f), colour, alpha, star: !streak);
            }
        }

        private static void Spark(VertexHelper mesh, Vector2 center, Vector2 direction,
            float length, float width, Color colour, float alpha, bool star = false)
        {
            int first = mesh.currentVertCount;
            var along = direction * length;
            var across = new Vector2(-direction.y, direction.x) * width;
            colour.a = alpha;
            float uv = star ? 2f : 0f;
            mesh.AddVert(center - along - across, colour, new Vector2(uv, 0));
            mesh.AddVert(center - along + across, colour, new Vector2(uv, 1));
            mesh.AddVert(center + along + across, colour, new Vector2(uv + 1, 1));
            mesh.AddVert(center + along - across, colour, new Vector2(uv + 1, 0));
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        private static float Signed(int index, int salt) => Noise(index, salt) * 2 - 1;

        private static float Noise(int index, int salt)
        {
            uint value = (uint)index * 747796405u + (uint)salt * 2891336453u;
            value = (value ^ (value >> 16)) * 0x7feb352du;
            value = (value ^ (value >> 15)) * 0x846ca68bu;
            return ((value ^ (value >> 16)) & 0xffff) / 65535f;
        }
    }
}
