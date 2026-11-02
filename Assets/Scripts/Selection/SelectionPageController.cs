using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhythmRehab
{
    public enum SelectionPage { Mode, Height, Difficulty }

    public sealed class SelectionPageController : MonoBehaviour
    {
        [SerializeField] private SelectionPage page;
        [SerializeField] private SongCatalog catalog;
        [SerializeField] private TMP_Text songContext;
        [SerializeField] private TMP_Text selectedLabel;
        [SerializeField] private Outline[] choiceOutlines;
        [SerializeField] private GameObject[] choiceBadges;
        [SerializeField] private ModeSelectionPreview modePreview;

        private void Start()
        {
            if (SessionSelection.Song == null) SessionSelection.SelectSong(catalog.songs[0]);
            var song = SessionSelection.Song;
            songContext.text = $"{song.title}  ·  {song.artist}  ·  {song.bpm:0} BPM\nTempo placeholder — song audio can be added later.";
            Refresh();
        }
        public void Therapy()
        {
            if (IrisSceneTransition.IsBusy) return;
            SessionSelection.SelectMode(SessionMode.Therapy);
            Refresh();
            if (modePreview) modePreview.Show(SessionMode.Therapy);
        }
        public void Free()
        {
            if (IrisSceneTransition.IsBusy) return;
            SessionSelection.SelectMode(SessionMode.Free);
            Refresh();
            if (modePreview) modePreview.Show(SessionMode.Free);
        }
        public void ContinueMode()
        {
            if (page != SelectionPage.Mode || IrisSceneTransition.IsBusy || (modePreview && modePreview.IsTransitioning)) return;
            IrisSceneTransition.Load(SessionSelection.Mode == SessionMode.Therapy ? "ExerciseSelection" : "DifficultySelection");
        }
        public void Choose(int index)
        {
            if (index < 0 || index > 2 || page == SelectionPage.Mode) return;
            if (page == SelectionPage.Height) SessionSelection.SelectHeight((HeightLevel)index);
            else SessionSelection.SelectDifficulty((FreeDifficulty)index);
            Refresh();
        }
        private void Refresh()
        {
            int index = page == SelectionPage.Mode ? (int)SessionSelection.Mode :
                page == SelectionPage.Height ? (int)SessionSelection.Height : (int)SessionSelection.Difficulty;
            for (int i = 0; i < choiceOutlines.Length; i++)
            {
                choiceOutlines[i].enabled = i == index;
                choiceBadges[i].SetActive(i == index);
            }
            selectedLabel.text = page == SelectionPage.Mode ?
                (SessionSelection.Mode == SessionMode.Therapy ? "SELECTED: THERAPY MODE\nChoose your target height next." : "SELECTED: FREE MODE\nChoose your difficulty next.") :
                page == SelectionPage.Height ? $"SELECTED: {SessionSelection.Height.ToString().ToUpperInvariant()} HEIGHT  ·  All notes stay at this level." : $"SELECTED: {SessionSelection.Difficulty.ToString().ToUpperInvariant()}  ·  Varied heights between the guide lines.";
        }
        public void BeginGame()
        {
            if (IrisSceneTransition.IsBusy) return;
            SessionSelection.SelectMode(page == SelectionPage.Height ? SessionMode.Therapy : SessionMode.Free);
            SessionSelection.Confirm();
            IrisSceneTransition.Load("Gameplay");
        }
        public void Back()
        {
            if (modePreview && modePreview.IsOpen) { modePreview.Close(); return; }
            IrisSceneTransition.Load(page == SelectionPage.Mode ? "SongSelection" : "ModeSelection");
        }
        public void Home() => IrisSceneTransition.Load("MainMenu");
    }
}

