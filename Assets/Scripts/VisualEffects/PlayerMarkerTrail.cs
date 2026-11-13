using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace RhythmRehab
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PlayerMarkerTrail : MaskableGraphic
    {
        [SerializeField] private PrototypeGameplay gameplay;
        [SerializeField] private RectTransform marker;
        [SerializeField] private Sprite sparkle;

        private struct Spark
        {
            public Vector2 position, velocity;
            public float remaining, size;
        }

        private const float Lifetime = .55f;
        private const float PulseDuration = .5f;
        private readonly Spark[] sparks = new Spark[24];
        private Vector2 previous;
        private float emission;
        private float outlineTime;
        private float pulseRemaining;
        private bool showOutline;
        private int next;
        private bool hasPrevious;

        public override Texture mainTexture => sparkle ? sparkle.texture : Texture2D.whiteTexture;

        protected override void OnEnable()
        {
            base.OnEnable();
            System.Array.Clear(sparks, 0, sparks.Length);
            hasPrevious = false;
            showOutline = false;
            outlineTime = 0;
            pulseRemaining = 0;
            emission = 1;
            next = 0;
        }

        public void PulseOutline()
        {
            pulseRemaining = PulseDuration;
            SetVerticesDirty();
        }

        private void LateUpdate()
        {
            if (!gameplay || !marker || gameplay.IsPaused || IrisSceneTransition.IsBusy)
            {
                hasPrevious = false;
                return;
            }

            float delta = Mathf.Min(Time.unscaledDeltaTime, .05f);
            outlineTime += delta;
            pulseRemaining = Mathf.Max(0, pulseRemaining - Time.unscaledDeltaTime);
            showOutline = true;
            Vector2 center = rectTransform.InverseTransformPoint(marker.TransformPoint(marker.rect.center));
            if (!hasPrevious) previous = center;
            hasPrevious = true;

            for (int i = 0; i < sparks.Length; i++)
            {
                ref Spark spark = ref sparks[i];
                if (spark.remaining <= 0) continue;
                spark.remaining -= delta;
                spark.position += spark.velocity * delta;
                spark.position.y = Mathf.MoveTowards(spark.position.y, center.y, 24 * delta);
            }

            float movement = Mathf.Abs(center.y - previous.y);
            emission += 20 * delta + Mathf.Min(movement / 22, 2);
            int count = Mathf.FloorToInt(emission);
            emission -= count;
            for (int i = 0; i < count; i++)
            {
                Vector2 origin = Vector2.Lerp(previous, center, (i + 1f) / count);
                sparks[next] = new Spark
                {
                    position = origin + new Vector2(-30, Mathf.Sin(next * 2.4f) * 19),
                    velocity = new Vector2(-50 - next % 5 * 6, Mathf.Cos(next * 2.4f) * 36),
                    remaining = Lifetime,
                    size = 16 + next % 4 * 3
                };
                next = (next + 1) % sparks.Length;
            }
            previous = center;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Vector4 uv = sparkle ? DataUtility.GetOuterUV(sparkle) : new Vector4(0, 0, 1, 1);
            foreach (Spark spark in sparks)
            {
                if (spark.remaining <= 0) continue;
                float life = spark.remaining / Lifetime;
                float radius = spark.size * Mathf.Lerp(.22f, .5f, life);
                Color tint = new Color(1, 1, 1, .9f * life);
                AddSpark(mesh, uv, spark.position, radius, tint);
            }
            if (showOutline && marker)
            {
                float pulse = pulseRemaining / PulseDuration;
                float radius = (marker.rect.width * .5f + 10) * (1 + .38f * pulse * pulse);
                IncomingNoteSparks.AddOutline(mesh, uv, previous, radius, outlineTime * 3.6f, Color.white);
            }
        }

        private static void AddSpark(VertexHelper mesh, Vector4 uv, Vector2 center, float radius, Color tint)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(center + new Vector2(-radius, -radius), tint, new Vector2(uv.x, uv.y));
            mesh.AddVert(center + new Vector2(-radius, radius), tint, new Vector2(uv.x, uv.w));
            mesh.AddVert(center + new Vector2(radius, radius), tint, new Vector2(uv.z, uv.w));
            mesh.AddVert(center + new Vector2(radius, -radius), tint, new Vector2(uv.z, uv.y));
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first + 2, first + 3, first);
        }
    }
}
