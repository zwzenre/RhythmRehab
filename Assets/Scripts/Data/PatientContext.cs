using System;
using UnityEngine;

namespace RhythmRehab
{
    [Serializable]
    public sealed class PatientProfile
    {
        public string id;
        public string displayName;
        public string documentId;
        public bool IsGuest => string.IsNullOrEmpty(id);
        public static PatientProfile Guest => new PatientProfile { id = "", displayName = "Guest" };
    }

    public static class PatientContext
    {
        public static string PatientId { get; private set; } = "";
        public static string DisplayName { get; private set; } = "Guest";
        public static bool IsGuest => string.IsNullOrEmpty(PatientId);
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { PatientId = ""; DisplayName = "Guest"; Changed = null; }

        public static void Select(PatientProfile patient)
        {
            PatientId = patient?.id ?? "";
            DisplayName = IsGuest ? "Guest" : CleanName(patient.displayName);
            SessionSummary.Last = null;
            Changed?.Invoke();
        }

        public static string CleanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unnamed patient";
            name = name.Trim().Replace('<', '‹').Replace('>', '›').Replace('\n', ' ').Replace('\r', ' ');
            return name.Length > 100 ? name.Substring(0, 100) : name;
        }
    }
}
