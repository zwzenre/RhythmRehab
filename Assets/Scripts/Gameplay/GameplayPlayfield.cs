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
        private bool[] usedSlots;
        private static readonly Color Low = new Color(.3f, 1, .45f), Mid = new Color(1, .75f, .15f), High = new Color(1, .3f, .5f);
        public float JudgementX => judgementX;
        public float SpawnX => spawnX;
        public float LaneSpacing => laneSpacing;
        public int Capacity => noteViews.Length;
        public RectTransform Marker => playerMarker;
        public Color MissColour => new Color(.65f, .74f, .88f);
        public bool HasCapacity(int count) => noteViews != null && noteImages != null && noteViews.Length >= count && noteImages.Length >= count;
        public bool CanDisplay(ChartNote[] chart, float bpm, float travel, double hitWindow, float offset)
        {
            if (noteViews == null || noteViews.Length == 0 || noteImages == null || noteImages.Length < noteViews.Length) return false;
            float seconds = 60f / Mathf.Max(1,bpm);
            // A slot is reused only after its earlier note and tail have left the playfield.
            for (int i = Capacity; i < chart.Length; i++)
                if (chart[i].beat * seconds - travel <= chart[i-Capacity].EndBeat * seconds + hitWindow + Mathf.Max(0,offset)) return false;
            return true;
        }
        public void HideAllNotes() { foreach (var view in noteViews) view.gameObject.SetActive(false); }
        public void Initialize(ChartNote[] chart)
        {
            HideAllNotes();
            usedSlots = new bool[Capacity];
            for (int i = 0; i < Capacity; i++) if (holdViews != null && i < holdViews.Length && holdViews[i]) holdViews[i].Hide();
        }
        public bool ReadMouseHeight(out float height)
        {
            height = 0;
            if (Mouse.current == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(playfield, Mouse.current.position.ReadValue(), null, out var local)) return false;
            height = Mathf.InverseLerp(-laneSpacing, laneSpacing, local.y); return true;
        }
        public void MoveMarker(float height) { playerMarker.anchoredPosition = PositionAt(height); playerMarker.GetComponent<UnityEngine.UI.Image>().color = Color.white; }
        public Vector2 PositionAt(float height) => new Vector2(judgementX, Mathf.Lerp(-laneSpacing, laneSpacing, height));
        public Color HeightColor(float height) => height <= ChartNote.MidTargetHeight
            ? Color.Lerp(Low, Mid, Mathf.InverseLerp(ChartNote.LowTargetHeight, ChartNote.MidTargetHeight, height))
            : Color.Lerp(Mid, High, Mathf.InverseLerp(ChartNote.MidTargetHeight, 1f, height));
        public void HideNote(int index) => noteViews[index % Capacity].gameObject.SetActive(false);
        public void Render(GameplayChartRunner runner, float time)
        {
            System.Array.Clear(usedSlots,0,usedSlots.Length);
            for (int i = 0; i < runner.Chart.Length; i++)
            {
                if (runner.IsResolved(i)) continue;
                var note = runner.Chart[i]; float remaining = note.beat * runner.SecondsPerBeat - time;
                bool holding = i == runner.ActiveHoldIndex;
                bool visible = holding || remaining <= runner.ApproachSeconds && remaining + runner.JudgementOffset >= -runner.HitWindow;
                if (!visible) continue;
                int slot = i % Capacity; usedSlots[slot] = true;
                if (!noteViews[slot].gameObject.activeSelf) noteViews[slot].gameObject.SetActive(true);
                noteImages[slot].color = HeightColor(note.TargetHeight);
                noteViews[slot].anchoredPosition = new Vector2(holding ? judgementX : Mathf.LerpUnclamped(judgementX, spawnX, remaining / runner.ApproachSeconds), PositionAt(note.TargetHeight).y);
                if (holdViews == null || slot >= holdViews.Length || !holdViews[slot]) continue;
                if (!note.IsHold) { holdViews[slot].Hide(); continue; }
                float seconds = holding ? Mathf.Max(0, (float)(runner.ActiveHold.EndTime - time)) : note.holdBeats * runner.SecondsPerBeat;
                holdViews[slot].Render(seconds * (spawnX - judgementX) / runner.ApproachSeconds, HeightColor(note.TargetHeight), holding, holding ? runner.ActiveHold.Progress(time) : 0);
            }
            for (int slot = 0; slot < Capacity; slot++) if (!usedSlots[slot] && noteViews[slot].gameObject.activeSelf) noteViews[slot].gameObject.SetActive(false);
        }
    }
}
