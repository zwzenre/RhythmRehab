using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RhythmRehab
{
    // Authored on SceneTransition. Reads Firebase only; no patient or session writes.
    public sealed class PatientDirectory : MonoBehaviour
    {
        public const string DocumentsUrl = "https://firestore.googleapis.com/v1/projects/patient-dashboard-c109b/databases/(default)/documents";
        public static PatientDirectory Instance { get; private set; }
        private List<PatientProfile> patients = new List<PatientProfile>();
        public IReadOnlyList<PatientProfile> Patients => patients;
        public bool IsLoading { get; private set; }
        public bool HasLoaded { get; private set; }
        public string Error { get; private set; }
        public event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = null;
        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(this); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }
        private int generation;
        private FirebaseLoginSession login;
        private void Start()
        {
            if (Instance != this) return;
            login = FirebaseLoginSession.Instance;
            if (login) login.Changed += AuthChanged;
            AuthChanged();
        }
        private void AuthChanged()
        {
            if (login && login.IsSignedIn) { if (!HasLoaded) Refresh(); return; }
            generation++; patients.Clear(); IsLoading = HasLoaded = false; Error = null; Changed?.Invoke();
        }
        public void Refresh() { if (login && login.IsSignedIn && !IsLoading) StartCoroutine(ReadPatients()); }

        private IEnumerator ReadPatients()
        {
            int attempt = generation;
            IsLoading = true; Error = null; Changed?.Invoke();
            string authToken = null;
            yield return login.GetToken(value => authToken = value);
            if (attempt != generation || string.IsNullOrEmpty(authToken)) yield break;
            var found = new Dictionary<string, PatientProfile>(StringComparer.Ordinal);
            var tokens = new HashSet<string>();
            string token = "";
            do
            {
                string url = DocumentsUrl + "/patients?pageSize=100&mask.fieldPaths=name&mask.fieldPaths=patientId&mask.fieldPaths=accountStatus";
                if (!string.IsNullOrEmpty(token)) url += "&pageToken=" + UnityWebRequest.EscapeURL(token);
                using (var request = UnityWebRequest.Get(url))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + authToken);
                    request.timeout = 15;
                    yield return request.SendWebRequest();
                    if (attempt != generation) yield break;
                    if (request.result != UnityWebRequest.Result.Success)
                    { Error = "Patient list unavailable. You can still play as Guest."; break; }
                    try
                    {
                        var response = JObject.Parse(request.downloadHandler.text);
                        foreach (var doc in response["documents"] as JArray ?? new JArray())
                        {
                            var patient = ParsePatient(doc);
                            if (patient != null) found[patient.id] = patient;
                        }
                        token = (string)response["nextPageToken"] ?? "";
                        if (token.Length > 0 && !tokens.Add(token)) throw new FormatException("Repeated page token");
                    }
                    catch (Exception) { Error = "Patient list could not be read. Please try Refresh."; break; }
                }
            } while (!string.IsNullOrEmpty(token));
            if (Error == null)
            {
                patients = found.Values.OrderBy(p => p.displayName, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.id, StringComparer.Ordinal).ToList();
                HasLoaded = true;
            }
            IsLoading = false; Changed?.Invoke();
        }

        public static PatientProfile ParsePatient(JToken document)
        {
            var fields = document?["fields"];
            string resource = (string)document?["name"];
            if (fields == null || string.IsNullOrEmpty(resource)) return null;
            string docId = Uri.UnescapeDataString(resource.Substring(resource.LastIndexOf('/') + 1));
            string id = Text(fields, "patientId");
            if (string.IsNullOrWhiteSpace(id)) id = docId;
            if (string.IsNullOrWhiteSpace(id)) return null;
            return new PatientProfile { id = id, documentId = docId, displayName = PatientContext.CleanName(Text(fields, "name")) };
        }

        public void LoadHistory(string patientId, Action<List<SessionSummary>, string> completed)
            => StartCoroutine(ReadHistory(patientId, completed));

        private IEnumerator ReadHistory(string patientId, Action<List<SessionSummary>, string> completed)
        {
            int attempt = generation;
            var found = new Dictionary<string, SessionSummary>(StringComparer.Ordinal);
            if (!login || !login.IsSignedIn || !patients.Any(p => p.id == patientId))
            { completed(new List<SessionSummary>(), "Sign in and choose a patient first."); yield break; }
            string authToken = null;
            yield return login.GetToken(value => authToken = value);
            if (attempt != generation || string.IsNullOrEmpty(authToken))
            { completed(new List<SessionSummary>(), "Please sign in again."); yield break; }
            if (string.IsNullOrEmpty(patientId)) { completed(new List<SessionSummary>(), null); yield break; }
            string error = null;
            // Both spellings exist in the shared portal schema. Deduplicate by document ID.
            foreach (string field in new[] { "patientId", "patientID" })
            {
                var body = new JObject { ["structuredQuery"] = new JObject
                {
                    ["from"] = new JArray(new JObject { ["collectionId"] = "rehabilitationSessions" }),
                    ["select"] = new JObject { ["fields"] = new JArray(new[]
                    {
                        "patientId", "patientID", "gameID", "songTitle", "sessionID", "modeLabel", "startTime", "endTime", "createdAt",
                        "durationSeconds", "duration", "finalScore", "rhythmAccuracyPercent", "completionRate", "score", "perfect", "great", "good", "miss",
                        "totalNotes", "bestCombo", "sensorHeightControl", "endedEarly", "sessionStatus"
                    }.Select(name => new JObject { ["fieldPath"] = name })) },
                    ["where"] = new JObject { ["fieldFilter"] = new JObject
                    {
                        ["field"] = new JObject { ["fieldPath"] = field }, ["op"] = "EQUAL",
                        ["value"] = new JObject { ["stringValue"] = patientId }
                    }}
                }};
                using (var request = new UnityWebRequest(DocumentsUrl + ":runQuery", "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body.ToString()));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + authToken); request.timeout = 20;
                    yield return request.SendWebRequest();
                    if (attempt != generation) { completed(new List<SessionSummary>(), "Signed out."); yield break; }
                    if (request.result != UnityWebRequest.Result.Success)
                    { error = "Cloud history unavailable. Showing saved local sessions."; continue; }
                    try
                    {
                        foreach (var item in JArray.Parse(request.downloadHandler.text))
                        {
                            var session = ParseSession(item["document"], patientId);
                            if (session != null) found[session.sessionId] = session;
                        }
                    }
                    catch (Exception) { error = "Cloud history could not be read. Showing saved local sessions."; }
                }
            }
            completed(found.Values.OrderByDescending(s => s.endedUtc).ToList(), error);
        }

        public static SessionSummary ParseSession(JToken document, string patientId)
        {
            var f = document?["fields"];
            if (f == null || Text(f, "gameID") != "GAME003" || string.IsNullOrWhiteSpace(Text(f, "songTitle"))) return null;
            if (Text(f, "patientId") != patientId && Text(f, "patientID") != patientId) return null;
            string ended = Timestamp(f, "endTime") ?? (string)f["createdAt"]?["timestampValue"];
            if (!DateTimeOffset.TryParse(ended, out var date)) return null;
            string resource = (string)document["name"] ?? "";
            string id = Text(f, "sessionID");
            if (string.IsNullOrEmpty(id)) id = resource.Substring(resource.LastIndexOf('/') + 1);
            if (string.IsNullOrEmpty(id)) return null;
            float accuracy = Number(f, "finalScore", -1);
            if (accuracy < 0) accuracy = Number(f, "rhythmAccuracyPercent", -1);
            var s = new SessionSummary
            {
                sessionId = id, patientId = patientId, isCloudRecord = true,
                songTitle = Text(f, "songTitle"), modeLabel = Text(f, "modeLabel"),
                endedUtc = date.ToUniversalTime().ToString("O"), startedUtc = Timestamp(f, "startTime"),
                duration = Mathf.Max(0, Number(f, "durationSeconds", Number(f, "duration"))),
                perfect = (int)Number(f, "perfect"), great = (int)Number(f, "great"), good = (int)Number(f, "good"), miss = (int)Number(f, "miss"),
                totalNotes = (int)Number(f, "totalNotes"), bestCombo = (int)Number(f, "bestCombo"),
                cloudAccuracy = accuracy < 0 ? -1 : Mathf.Clamp01(accuracy / 100),
                cloudCompletion = Number(f, "completionRate", -1) < 0 ? -1 : Mathf.Clamp01(Number(f, "completionRate") / 100),
                sensorHeightControl = (bool?)f["sensorHeightControl"]?["booleanValue"] ?? false
            };
            if (string.IsNullOrEmpty(s.modeLabel)) s.modeLabel = "RhythmRehab";
            s.score = (int)Number(f, "score", accuracy < 0 ? 0 : Mathf.RoundToInt(RhythmRules.MaximumScore * s.cloudAccuracy));
            s.endedEarly = (bool?)f["endedEarly"]?["booleanValue"] ?? (Text(f, "sessionStatus") == "EndedEarly" || s.Completion < 1);
            return s;
        }
        private static string Text(JToken fields, string name) => (string)fields?[name]?["stringValue"] ?? "";
        private static float Number(JToken fields, string name, float fallback = 0)
        {
            var value = fields?[name];
            string text = (string)(value?["doubleValue"] ?? value?["integerValue"]);
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && !float.IsNaN(n) && !float.IsInfinity(n) ? n : fallback;
        }
        private static string Timestamp(JToken fields, string name)
        {
            var value = fields?[name];
            string text = (string)(value?["integerValue"] ?? value?["doubleValue"]);
            if (!long.TryParse(text, out var milliseconds)) return null;
            try { return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).ToString("O"); }
            catch (ArgumentOutOfRangeException) { return null; }
        }
        private void OnDestroy() { if (login) login.Changed -= AuthChanged; if (Instance == this) Instance = null; }
    }
}
