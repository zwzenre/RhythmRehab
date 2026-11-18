using UnityEngine;

namespace RhythmRehab
{
    [CreateAssetMenu(menuName = "RhythmRehab/Firebase Client Config", fileName = "FirebaseClientConfig")]
    public sealed class FirebaseClientConfig : ScriptableObject
    {
        [SerializeField] private string apiKey;

        public string ApiKey => apiKey?.Trim();
    }
}
