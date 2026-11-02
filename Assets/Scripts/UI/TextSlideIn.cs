using TMPro;
using UnityEngine;

namespace RhythmRehab
{
    // Animate the text mesh, preserving RectTransforms used by layouts, scroll views and graphs.
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class TextSlideIn : MonoBehaviour
    {
        [SerializeField] private Vector2 fromOffset = new Vector2(-80, 0);
        [SerializeField, Min(.05f)] private float duration = .95f;
        [SerializeField, Min(0)] private float delay;
        private TMP_Text label;
        private Vector2 offset;
        private float opacity;
        private bool started, running;
        private int enabledFrame;
        private float startTime;
        public bool IsAnimating => running;
        public Vector2 CurrentOffset => offset;
        public float CurrentOpacity => opacity;
        public void CompleteImmediately()
        { running=false;offset=Vector2.zero;opacity=1;RefreshMesh(); }

        private void OnEnable()
        {
            label = GetComponent<TMP_Text>();
            offset = fromOffset;
            opacity = 0;
            started = false;
            running = true;
            enabledFrame = Time.frameCount;
            label.OnPreRenderText += ApplyOffset;
            RefreshMesh();
        }

        private void LateUpdate()
        {
            if (!running) return;
            // Presenters populate text and graph coordinates first. Use unscaled time for menus.
            if (!started && Time.frameCount > enabledFrame && !IrisSceneTransition.IsBusy)
            { started = true; startTime = Time.unscaledTime; }
            if (started)
            {
                float progress = Mathf.Clamp01((Time.unscaledTime - startTime - delay) / Mathf.Max(.05f, duration));
                float ease = 1 - Mathf.Pow(1 - progress, 3);
                offset = Vector2.LerpUnclamped(fromOffset, Vector2.zero, ease);
                opacity = Mathf.SmoothStep(0, 1, progress);
                if (progress >= 1) running = false;
            }
            RefreshMesh();
        }

        private void RefreshMesh()
        {
            if (!label) return;
            label.havePropertiesChanged = true;
            label.SetVerticesDirty();
        }

        private void ApplyOffset(TMP_TextInfo info)
        {
            if (offset == Vector2.zero && opacity >= 1) return;
            for (int i = 0; i < info.meshInfo.Length; i++)
            {
                var mesh = info.meshInfo[i];
                for (int v = 0; v < mesh.vertexCount; v++)
                {
                    mesh.vertices[v] += (Vector3)offset;
                    var tint = mesh.colors32[v];
                    tint.a = (byte)Mathf.RoundToInt(tint.a * opacity);
                    mesh.colors32[v] = tint;
                }
            }
        }

        private void OnDisable()
        {
            if (!label) return;
            label.OnPreRenderText -= ApplyOffset;
            offset = Vector2.zero;
            opacity = 1;
            running = false;
            RefreshMesh();
        }
    }
}
