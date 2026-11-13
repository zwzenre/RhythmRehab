using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class SongCardView : MonoBehaviour
    {
        [SerializeField] private SongSelectionController owner;
        [SerializeField] private int songIndex;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text artist;
        [SerializeField] private TMP_Text tempo;
        [SerializeField] private Image cover;
        [SerializeField] private Outline selectionOutline;
        [SerializeField] private GameObject selectedBadge;
        public SongEntry Song { get; private set; }

        public void Bind(SongEntry song)
        {
            Song = song;
            title.text = song.title;
            artist.text = song.artist;
            tempo.text = song.audio ? $"{System.TimeSpan.FromSeconds(song.audio.length):m\\:ss}  ·  CLASSICAL" : $"{song.bpm:0} BPM  ·  TEMPO DEMO";
            cover.sprite = song.cover;
            cover.preserveAspect = song.cover != null;
            cover.color = song.cover ? Color.white : song.coverColor;
            foreach (var label in cover.GetComponentsInChildren<TMP_Text>(true)) label.gameObject.SetActive(!song.cover);
        }
        public void SetSelected(bool selected)
        {
            selectionOutline.enabled = selected;
            selectedBadge.SetActive(selected);
        }
        public void Select() => owner.SelectSong(songIndex);
    }
}
