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
            tempo.text = $"{song.bpm:0} BPM  ·  TEMPO DEMO";
            cover.color = song.coverColor;
        }
        public void SetSelected(bool selected)
        {
            selectionOutline.enabled = selected;
            selectedBadge.SetActive(selected);
        }
        public void Select() => owner.SelectSong(songIndex);
    }
}
