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
        [SerializeField] private TMP_Text[] difficultyNoteCounts;

        private void Start()
        {
            if (SessionSelection.Song == null) SessionSelection.SelectSong(catalog.songs[0]);
            var song = SessionSelection.Song;
            if (page == SelectionPage.Difficulty && songContext)
                songContext.text = song.audio ? $"{song.title}  ·  {song.artist}\nRecording: {song.recordingCredit}" : $"{song.title}  ·  {song.artist}  ·  {song.bpm:0} BPM";
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
            if (page == SelectionPage.Difficulty && SessionSelection.Song != null && difficultyNoteCounts != null)
                for (int i = 0; i < difficultyNoteCounts.Length && i < 3; i++)
                    if (difficultyNoteCounts[i]) difficultyNoteCounts[i].text = SessionSelection.BuildProfile(SessionSelection.Song, SessionMode.Free, HeightLevel.Low, (FreeDifficulty)i).chart.Length + " notes";
            int index = page == SelectionPage.Mode ? (int)SessionSelection.Mode :
                page == SelectionPage.Height ? (int)SessionSelection.Height : (int)SessionSelection.Difficulty;
            for (int i = 0; i < choiceOutlines.Length; i++)
            {
                choiceOutlines[i].enabled = i == index;
                choiceBadges[i].SetActive(i == index);
            }
            selectedLabel.text = page == SelectionPage.Mode ?
                (SessionSelection.Mode == SessionMode.Therapy ? "THERAPY MODE  ·  Choose a target height next" : "FREE MODE  ·  Choose a difficulty next") :
                page == SelectionPage.Height ? $"{SessionSelection.Height.ToString().ToUpperInvariant()} HEIGHT  ·  One target for all notes" : $"SELECTED: {SessionSelection.Difficulty.ToString().ToUpperInvariant()}  ·  Varied heights between the guide lines.";
        }
        public void BeginGame()
        {
            if (IrisSceneTransition.IsBusy) return;
            SessionSelection.SelectMode(page == SelectionPage.Height ? SessionMode.Therapy : SessionMode.Free);
            SessionSelection.Confirm();
            IrisSceneTransition.Load(page == SelectionPage.Height ? "DrawingActivity" : "Gameplay");
        }
        public void Back()
        {
            if (modePreview && modePreview.IsOpen) { modePreview.Close(); return; }
            IrisSceneTransition.Load(page == SelectionPage.Mode ? "SongSelection" : "ModeSelection");
        }
        public void Home() => IrisSceneTransition.Load("MainMenu");
    }
}

