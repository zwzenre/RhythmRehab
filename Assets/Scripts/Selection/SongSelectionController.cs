using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class SongSelectionController : MonoBehaviour
    {
        [SerializeField] private SongCatalog catalog;
        [SerializeField] private SongCardView[] cards;
        [SerializeField] private ScrollRect songScroll;
        [SerializeField] private TMP_InputField search;
        [SerializeField] private TMP_Text filterLabel;
        [SerializeField] private TMP_Text selectionLabel;
        [SerializeField] private TMP_Text browseLabel;
        [SerializeField] private GameObject emptyMessage;
        [SerializeField] private Button continueButton;
        private int filter;

        private void Start()
        {
            if (catalog == null || catalog.songs == null || catalog.songs.Length != cards.Length)
            { Debug.LogError("Song catalog needs one authored card per song.", this); enabled = false; return; }
            for (int i = 0; i < cards.Length; i++) cards[i].Bind(catalog.songs[i]);
            if (SessionSelection.Song == null) SessionSelection.SelectSong(catalog.songs[0]);
            RefreshSelection();
            Filter(search.text);
        }

        public void SelectSong(int index)
        {
            if (index < 0 || index >= catalog.songs.Length) return;
            SessionSelection.SelectSong(catalog.songs[index]);
            RefreshSelection();
        }
        private void RefreshSelection()
        {
            var song = SessionSelection.Song;
            foreach (var card in cards) card.SetSelected(card.Song != null && song != null && card.Song.id == song.id);
            selectionLabel.text = song == null ? "Choose a song to continue." : $"SELECTED: {song.title}  ·  {song.bpm:0} BPM";
            continueButton.interactable = song != null;
        }
        public void Filter(string query)
        {
            query = (query ?? "").Trim();
            int visible = 0;
            foreach (var card in cards)
            {
                var song = card.Song;
                if (song == null) continue;
                bool matches = (query.Length == 0 || song.title.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0 || song.artist.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    && (filter == 0 || filter == 1 && song.bpm <= 110 || filter == 2 && song.bpm > 110);
                card.gameObject.SetActive(matches);
                if (matches) visible++;
            }
            emptyMessage.SetActive(visible == 0);
            browseLabel.text = $"{visible} SONGS  ·  DRAG OR SCROLL TO BROWSE";
            Canvas.ForceUpdateCanvases();
            songScroll.StopMovement();
            songScroll.horizontalNormalizedPosition = 0;
        }
        public void CycleFilter()
        {
            filter = (filter + 1) % 3;
            filterLabel.text = new[] { "ALL TEMPOS", "CALM  ≤110 BPM", "UPBEAT  >110 BPM" }[filter];
            Filter(search.text);
        }
        public void Previous() => ScrollBy(-0.5f);
        public void Next() => ScrollBy(0.5f);
        private void ScrollBy(float direction)
        {
            songScroll.StopMovement();
            songScroll.horizontalNormalizedPosition = Mathf.Clamp01(songScroll.horizontalNormalizedPosition + direction);
        }
        public void Continue()
        {
            if (SessionSelection.Song != null) IrisSceneTransition.Load("ModeSelection");
        }
        public void Home() => IrisSceneTransition.Load("MainMenu");
    }
}

