using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class DrawingActivityPresenter : MonoBehaviour
    {
        [SerializeField] private DrawingSurface surface;
        [SerializeField] private Color[] colours;
        [SerializeField] private Outline[] colourOutlines, sizeOutlines, toolOutlines;
        [SerializeField] private Button undoButton;
        private int selectedColour, selectedSize = 1;
        private bool eraser;
        private void Start()
        {
            surface.Changed += Refresh;
            Colour(0); Size(1); Refresh();
        }
        public void Colour(int index)
        {
            if (index < 0 || index >= colours.Length) return;
            selectedColour = index; eraser = false; surface.SetInk(colours[index]); Refresh();
        }
        public void Size(int index) { selectedSize = Mathf.Clamp(index, 0, 2); surface.SetSize(selectedSize); Refresh(); }
        public void Pen() { eraser = false; surface.SetEraser(false); Refresh(); }
        public void Eraser() { eraser = true; surface.SetEraser(true); Refresh(); }
        public void Undo() => surface.Undo();
        public void Clear() => surface.Clear();
        private void Refresh()
        {
            for (int i = 0; i < colourOutlines.Length; i++) colourOutlines[i].enabled = i == selectedColour && !eraser;
            for (int i = 0; i < sizeOutlines.Length; i++) sizeOutlines[i].enabled = i == selectedSize;
            for (int i = 0; i < toolOutlines.Length; i++) toolOutlines[i].enabled = i == (eraser ? 1 : 0);
            undoButton.interactable = surface.CanUndo;
        }
        public void BeginGame()
        {
            if (!IrisSceneTransition.IsBusy && SessionSelection.Configured) IrisSceneTransition.Load("Gameplay");
        }
        public void Back() => IrisSceneTransition.Load("ExerciseSelection");
        private void OnDestroy() { if (surface) surface.Changed -= Refresh; }
    }
}
