using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RhythmRehab
{
    // Plain snapshots: changing the current result cannot alter a saved session.
    public sealed class SessionHistoryStore
    {
        [Serializable] private sealed class Document
        {
            public int version = 1;
            public List<SessionSummary> sessions = new List<SessionSummary>();
        }
        private static SessionHistoryStore defaultStore;
        public static SessionHistoryStore Default => defaultStore ??
            (defaultStore = new SessionHistoryStore(Path.Combine(Application.persistentDataPath, "session-history.json")));
        public string FilePath { get; }
        public SessionHistoryStore(string path) { FilePath = path; }

        public List<SessionSummary> Load(out string error)
        {
            error = null;
            try
            {
                if (!File.Exists(FilePath)) return new List<SessionSummary>();
                var raw = File.ReadAllText(FilePath);
                var doc = JsonUtility.FromJson<Document>(raw);
                if (doc == null || doc.version != 1 || doc.sessions == null || !raw.Contains("\"sessions\""))
                    throw new InvalidDataException("Unrecognized history file. The original file has been preserved.");
                if (doc.sessions.Exists(s => s == null || string.IsNullOrEmpty(s.sessionId) ||
                    !DateTimeOffset.TryParse(s.endedUtc, out _)))
                    throw new InvalidDataException("Invalid session record. The original file has been preserved.");
                doc.sessions.Sort((a,b) => DateTimeOffset.Parse(b.endedUtc).CompareTo(DateTimeOffset.Parse(a.endedUtc)));
                return doc.sessions;
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { error = ex.Message; return new List<SessionSummary>(); }
        }

        public bool TrySave(SessionSummary summary, out string error)
        {
            if (summary == null || string.IsNullOrEmpty(summary.sessionId) || !DateTimeOffset.TryParse(summary.endedUtc, out _))
            { error = "The session has no valid identifier or end time."; return false; }
            var sessions = Load(out error);
            if (error != null) return false; // Never overwrite unreadable history.
            if (sessions.Exists(s => s.sessionId == summary.sessionId)) return true;
            sessions.Add(JsonUtility.FromJson<SessionSummary>(JsonUtility.ToJson(summary)));
            var temp = FilePath + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath)));
                File.WriteAllText(temp, JsonUtility.ToJson(new Document { sessions = sessions }, true));
                if (File.Exists(FilePath)) File.Replace(temp, FilePath, null);
                else File.Move(temp, FilePath);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { error = ex.Message; return false; }
        }
    }
}
