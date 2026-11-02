using System;
using UnityEngine;

namespace RhythmRehab
{
    [Serializable]
    public sealed class SongEntry
    {
        public string id;
        public string title;
        public string artist = "RhythmRehab Demo";
        [Range(60, 180)] public float bpm = 120;
        public Color coverColor = new Color(0.4f, 0.2f, 0.8f);
    }

    [CreateAssetMenu(menuName = "RhythmRehab/Song Catalog")]
    public sealed class SongCatalog : ScriptableObject
    {
        public SongEntry[] songs;
    }
}
