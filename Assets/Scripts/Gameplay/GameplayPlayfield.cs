using UnityEngine;
using UnityEngine.InputSystem;

namespace RhythmRehab
{
    public sealed class GameplayPlayfield : MonoBehaviour
    {
        [SerializeField] private RectTransform playfield;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform[] noteViews;
        [SerializeField] private UnityEngine.UI.Image[] noteImages;
        [SerializeField] private HoldNoteView[] holdViews;
        [SerializeField] private float judgementX = -550;
        [SerializeField] private float spawnX = 900;
        [SerializeField] private float laneSpacing = 280;
        private static readonly Color Low = new Color(.3f, 1, .45f), Mid = new Color(1, .75f, .15f), High = new Color(1, .3f, .5f);
        public float JudgementX => judgementX;
        public float SpawnX => spawnX;
        public float LaneSpacing => laneSpacing;
        public int Capacity => noteViews.Length;
        public RectTransform Marker => playerMarker;
        public Color MissColour => new Color(.65f, .74f, .88f);
        public bool HasCapacity(int count) => noteViews != null && noteImages != null && noteViews.Length >= count && noteImages.Length >= count;
        public void HideAllNotes() { foreach (var view in noteViews) view.gameObject.SetActive(false); }
        public void Initialize(ChartNote[] chart)
        {
            HideAllNotes();
            for (int i = 0; i < chart.Length; i++) { noteImages[i].color = HeightColor(chart[i].TargetHeight); if (holdViews != null && i < holdViews.Length && holdViews[i]) holdViews[i].Hide(); }
        }
        public bool ReadMouseHeight(out float height)
        {
            height = 0;
            if (Mouse.current == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(playfield, Mouse.current.position.ReadValue(), null, out var local)) return false;
            height = Mathf.InverseLerp(-laneSpacing, laneSpacing, local.y); return true;
        }
        public void MoveMarker(float height) { playerMarker.anchoredPosition = PositionAt(height); playerMarker.GetComponent<UnityEngine.UI.Image>().color = Color.white; }
        public Vector2 PositionAt(float height) => new Vector2(judgementX, Mathf.Lerp(-laneSpacing, laneSpacing, height));
        public Color HeightColor(float height) => height <= .5f ? Color.Lerp(Low, Mid, Mathf.Clamp01(height) * 2) : Color.Lerp(Mid, High, (Mathf.Clamp01(height) - .5f) * 2);
        public void HideNote(int index) => noteViews[index].gameObject.SetActive(false);
        public void Render(GameplayChartRunner runner, float time)
        {
            for (int i = 0; i < runner.Chart.Length; i++)
            {
                if (runner.IsResolved(i)) continue;
                var note = runner.Chart[i]; float remaining = note.beat * runner.SecondsPerBeat - time;
                bool holding = i == runner.ActiveHoldIndex;
                bool visible = holding || remaining <= runner.ApproachSeconds && remaining + runner.JudgementOffset >= -runner.HitWindow;
                noteViews[i].gameObject.SetActive(visible);
                if (!visible) continue;
                noteViews[i].anchoredPosition = new Vector2(holding ? judgementX : Mathf.LerpUnclamped(judgementX, spawnX, remaining / runner.ApproachSeconds), PositionAt(note.TargetHeight).y);
                if (holdViews == null || i >= holdViews.Length || !holdViews[i]) continue;
                if (!note.IsHold) { holdViews[i].Hide(); continue; }
                float seconds = holding ? Mathf.Max(0, (float)(runner.ActiveHold.EndTime - time)) : note.holdBeats * runner.SecondsPerBeat;
                holdViews[i].Render(seconds * (spawnX - judgementX) / runner.ApproachSeconds, HeightColor(note.TargetHeight), holding, holding ? runner.ActiveHold.Progress(time) : 0);
            }
        }
    }
}
