using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class LeaderboardPresenter : MonoBehaviour
    {
        internal static SessionSummary OpenFromResults { get; set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetNavigation() => OpenFromResults = null;

        [SerializeField] private SongCatalog catalog;
        [SerializeField] private GameObject overlay;
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private TMP_Text title, status;
        [SerializeField] private Button[] difficultyTabs, inputTabs;
        [SerializeField] private TMP_Text[] names, scores;
        private bool transitioning;
        private int songIndex, requestVersion;
        private FreeDifficulty difficulty;
        private bool sensor;
        private GameObject previousSelection;
        public bool IsOpen => overlay.activeSelf;

        public void Show()
        {
            var song = SessionSelection.Song;
            if (song != null && !IrisSceneTransition.IsBusy) Open(song.id, difficulty, sensor);
        }

        internal void Open(string songId, FreeDifficulty category, bool sensorInput)
        {
            if (IsOpen || transitioning) return;
            songIndex = System.Array.FindIndex(catalog.songs, song => song.id == songId);
            if (songIndex < 0) return;
            difficulty = category;
            sensor = sensorInput;
            if (EventSystem.current)
            {
                previousSelection = EventSystem.current.currentSelectedGameObject;
                EventSystem.current.SetSelectedGameObject(null);
            }
            overlay.SetActive(true);
            Refresh();
            StartCoroutine(Animate(true));
        }

        public void Close()
        {
            if (!IsOpen || transitioning) return;
            requestVersion++;
            StartCoroutine(Animate(false));
        }

        private IEnumerator Animate(bool opening)
        {
            var rect = (RectTransform)panel.transform;
            transitioning = true;
            panel.interactable = false;
            panel.alpha = opening ? 0 : 1;
            yield return ModeSelectionPreview.SlideFade(panel, rect.anchoredPosition, 1, .28f,
                opening ? 0 : 1, opening ? 1 : 0);
            if (!opening) overlay.SetActive(false);
            panel.interactable = opening;
            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(opening ? difficultyTabs[(int)difficulty].gameObject : previousSelection);
            transitioning = false;
        }

        public void ChangeSong(int step)
        {
            if (!IsOpen || transitioning) return;
            songIndex = (songIndex + step + catalog.songs.Length) % catalog.songs.Length;
            Refresh();
        }

        public void SetDifficulty(int index)
        {
            if (!IsOpen || transitioning || index == (int)difficulty || index < 0 || index > 2) return;
            difficulty = (FreeDifficulty)index;
            Refresh();
        }

        public void SetInput(int index)
        {
            if (!IsOpen || transitioning || index < 0 || index > 1 || sensor == (index == 1)) return;
            sensor = index == 1;
            Refresh();
        }

        private void Refresh()
        {
            var song = catalog.songs[songIndex];
            title.text = song.title;
            MarkSelected(difficultyTabs, (int)difficulty);
            MarkSelected(inputTabs, sensor ? 1 : 0);
            for (int i = 0; i < names.Length; i++) names[i].transform.parent.gameObject.SetActive(false);
            status.text = "Loading rankings…";
            int version = ++requestVersion;
            StartCoroutine(FreeLeaderboard.Read(song.id, difficulty, sensor, (entries, error) =>
            {
                if (version != requestVersion || !IsOpen) return;
                status.text = error ?? (entries.Count == 0 ? "No scores published yet." : "");
                if (entries == null) return;
                for (int i = 0; i < Mathf.Min(entries.Count, names.Length); i++)
                {
                    names[i].text = entries[i].Name;
                    scores[i].text = (entries[i].Score / 10000f).ToString("P2");
                    names[i].transform.parent.gameObject.SetActive(true);
                }
            }));
        }

        private static void MarkSelected(Button[] buttons, int selected)
        {
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].GetComponent<Outline>().enabled = i == selected;
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }
    }
}
