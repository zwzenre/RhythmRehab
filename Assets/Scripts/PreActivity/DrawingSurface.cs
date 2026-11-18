using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RhythmRehab
{
    [RequireComponent(typeof(RawImage))]
    public sealed class DrawingSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IEndDragHandler
    {
        private const int Width = 1024, Height = 576, UndoLimit = 6;
        private readonly List<Color32[]> undo = new List<Color32[]>();
        private Color32[] pixels;
        private Texture2D texture;
        private RectTransform rect;
        private Color ink = new Color(.65f, .35f, 1);
        private float radius = 5;
        private bool erasing, drawing, hasPrevious, dirty;
        private int pointer;
        private Vector2 previous;
        public bool CanUndo => undo.Count > 0;
        public event System.Action Changed;

        private void Awake()
        {
            rect = (RectTransform)transform;
            texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { name = "Temporary drawing", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            pixels = new Color32[Width * Height];
            GetComponent<RawImage>().texture = texture;
            Upload();
        }
        public void SetInk(Color colour) { Finish(); ink = colour; erasing = false; }
        public void SetSize(int size) { Finish(); radius = size == 0 ? 3 : size == 1 ? 6 : 12; }
        public void SetEraser(bool value) { Finish(); erasing = value; }
        private bool Point(PointerEventData data, out Vector2 point)
        {
            point = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out var local) || !rect.rect.Contains(local)) return false;
            point = new Vector2((local.x - rect.rect.xMin) / rect.rect.width * (Width - 1), (local.y - rect.rect.yMin) / rect.rect.height * (Height - 1));
            return true;
        }
        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || drawing || IrisSceneTransition.IsBusy || !Point(data, out var point)) return;
            SaveUndo(); drawing = true; pointer = data.pointerId; previous = point; hasPrevious = true; Stamp(point); Changed?.Invoke();
        }
        public void OnDrag(PointerEventData data)
        {
            if (!drawing || data.pointerId != pointer || IrisSceneTransition.IsBusy) return;
            if (!Point(data, out var point)) { hasPrevious = false; return; }
            if (hasPrevious)
            {
                int steps = Mathf.CeilToInt(Vector2.Distance(previous, point) / Mathf.Max(1, radius * .4f));
                for (int i = 1; i <= steps; i++) Stamp(Vector2.Lerp(previous, point, i / (float)steps));
            }
            else Stamp(point);
            previous = point; hasPrevious = true;
        }
        public void OnPointerUp(PointerEventData data) { if (data.pointerId == pointer) Finish(); }
        public void OnEndDrag(PointerEventData data) { if (data.pointerId == pointer) Finish(); }
        private void Finish() { drawing = hasPrevious = false; }
        private void OnDisable() => Finish();
        private void OnApplicationFocus(bool focused) { if (!focused) Finish(); }
        private void Stamp(Vector2 point)
        {
            float r = erasing ? radius * 2 : radius;
            for (int y = Mathf.Max(0, Mathf.FloorToInt(point.y - r)); y <= Mathf.Min(Height - 1, Mathf.CeilToInt(point.y + r)); y++)
                for (int x = Mathf.Max(0, Mathf.FloorToInt(point.x - r)); x <= Mathf.Min(Width - 1, Mathf.CeilToInt(point.x + r)); x++)
                {
                    float coverage = Mathf.Clamp01(r - Vector2.Distance(new Vector2(x, y), point));
                    if (coverage <= 0) continue;
                    int index = y * Width + x;
                    Color old = pixels[index];
                    if (erasing) old.a *= 1 - coverage;
                    else
                    {
                        float alpha = coverage + old.a * (1 - coverage);
                        old = new Color((ink.r * coverage + old.r * old.a * (1 - coverage)) / alpha,
                            (ink.g * coverage + old.g * old.a * (1 - coverage)) / alpha,
                            (ink.b * coverage + old.b * old.a * (1 - coverage)) / alpha, alpha);
                    }
                    pixels[index] = old;
                }
            dirty = true;
        }
        private void SaveUndo()
        {
            if (undo.Count == UndoLimit) undo.RemoveAt(0);
            undo.Add((Color32[])pixels.Clone());
        }
        public void Undo()
        {
            Finish(); if (!CanUndo) return;
            pixels = undo[undo.Count - 1]; undo.RemoveAt(undo.Count - 1); dirty = true; Changed?.Invoke();
        }
        public void Clear()
        {
            Finish();
            if (!System.Array.Exists(pixels, p => p.a > 0)) return;
            SaveUndo(); System.Array.Clear(pixels, 0, pixels.Length); dirty = true; Changed?.Invoke();
        }
        private void LateUpdate() { if (dirty) Upload(); }
        private void Upload() { texture.SetPixels32(pixels); texture.Apply(false, false); dirty = false; }
        private void OnDestroy() { if (texture) Destroy(texture); undo.Clear(); }
    }
}
