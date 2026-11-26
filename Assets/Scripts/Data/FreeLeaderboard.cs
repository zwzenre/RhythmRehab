using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RhythmRehab
{
    internal static class FreeLeaderboard
    {
        public const int Size = 10;
        private const string Collection = "/rhythmRehabLeaderboards/";
        private const string Unavailable = "Leaderboard unavailable. Please try again.";

        internal enum PublishResult { Saved, KeptBest, Unavailable }

        internal readonly struct Entry
        {
            public readonly string Name;
            public readonly int Score;
            public Entry(string name, int score) { Name = name; Score = score; }
        }

        public static bool TryName(string value, out string name)
        {
            name = "";
            if (string.IsNullOrWhiteSpace(value)) return false;
            try { name = Regex.Replace(value.Normalize(NormalizationForm.FormKC).Trim(), @"\s+", " "); }
            catch (ArgumentException) { return false; }
            return name.Length <= 24 && Regex.IsMatch(name, @"\A[\p{L}\p{N}][\p{L}\p{N}\p{M} _-]*\z");
        }

        private static string EntriesUrl(string songId, FreeDifficulty difficulty, bool sensor)
        {
            if (string.IsNullOrEmpty(songId) || !Regex.IsMatch(songId, @"\A[a-z0-9_-]{1,100}\z") ||
                !Enum.IsDefined(typeof(FreeDifficulty), difficulty)) return null;
            return PatientDirectory.DocumentsUrl + Collection + songId + "~" + difficulty.ToString().ToLowerInvariant()
                + (sensor ? "~sensor" : "~keyboard") + "/entries";
        }

        private static string NameKey(string name)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(name.ToUpperInvariant())))
                    .Replace("-", "").ToLowerInvariant();
        }

        public static IEnumerator Read(string songId, FreeDifficulty difficulty, bool sensor, Action<List<Entry>, string> completed)
        {
            string url = EntriesUrl(songId, difficulty, sensor);
            if (url == null) { completed(null, Unavailable); yield break; }
            string query = "?orderBy=score%20desc&mask.fieldPaths=name&mask.fieldPaths=score";
            string pageToken = "";
            var entries = new List<Entry>(Size);
            var seenPages = new HashSet<string>();
            do
            {
                using (var request = UnityWebRequest.Get(url + query + "&pageSize=" + (Size - entries.Count) + pageToken))
                {
                    request.timeout = 12;
                    yield return request.SendWebRequest();
                    if (request.responseCode == 404) break;
                    var data = ReadJson(request);
                    if (request.result != UnityWebRequest.Result.Success || data == null)
                    { completed(null, Unavailable); yield break; }
                    if (data["documents"] is JArray documents)
                        foreach (var document in documents)
                        {
                            if (entries.Count == Size) break;
                            var fields = document["fields"];
                            if (TryName((string)fields?["name"]?["stringValue"], out var name) &&
                                TryScore(fields, out var score)) entries.Add(new Entry(name, score));
                        }
                    string next = (string)data["nextPageToken"];
                    pageToken = string.IsNullOrEmpty(next) ? "" : "&pageToken=" + UnityWebRequest.EscapeURL(next);
                }
            } while (entries.Count < Size && pageToken.Length > 0 && seenPages.Add(pageToken));
            completed(entries, null);
        }

        public static IEnumerator Publish(SessionSummary session, string value, Action<PublishResult> completed)
        {
            if (session?.CanPublishLeaderboard != true || !TryName(value, out var name))
            { completed(PublishResult.Unavailable); yield break; }
            string entries = EntriesUrl(session.songId, session.freeDifficulty, session.playedWithSensor);
            if (entries == null) { completed(PublishResult.Unavailable); yield break; }
            string url = entries + "/" + NameKey(name);
            int score = ScoreFor(session);
            yield return SaveBestScore(url, name, score, completed);
        }

        internal static int ScoreFor(SessionSummary session) =>
            Mathf.Clamp(Mathf.RoundToInt(session.Accuracy * 10000), 0, 10000);

        private static IEnumerator SaveBestScore(string url, string name, int score, Action<PublishResult> completed)
        {
            string scoreValue = score.ToString(CultureInfo.InvariantCulture);
            var body = new JObject { ["fields"] = new JObject
            {
                ["name"] = new JObject { ["stringValue"] = name },
                ["score"] = new JObject { ["integerValue"] = scoreValue },
            }};
            for (int attempt = 0; attempt < 3; attempt++)
            {
                string condition;
                using (var read = UnityWebRequest.Get(url))
                {
                    read.timeout = 12;
                    yield return read.SendWebRequest();
                    if (read.responseCode == 404) condition = "exists=false";
                    else
                    {
                        var existing = ReadJson(read);
                        string updateTime = (string)existing?["updateTime"];
                        if (read.result != UnityWebRequest.Result.Success || string.IsNullOrEmpty(updateTime))
                        { completed(PublishResult.Unavailable); yield break; }
                        if (TryScore(existing["fields"], out var previous) && previous >= score)
                        { completed(PublishResult.KeptBest); yield break; }
                        condition = "updateTime=" + UnityWebRequest.EscapeURL(updateTime);
                    }
                }
                using (var write = new UnityWebRequest(url + "?currentDocument." + condition, "PATCH"))
                {
                    write.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString(Formatting.None)));
                    write.downloadHandler = new DownloadHandlerBuffer();
                    write.SetRequestHeader("Content-Type", "application/json");
                    write.timeout = 12;
                    yield return write.SendWebRequest();
                    if (write.result == UnityWebRequest.Result.Success)
                    { completed(PublishResult.Saved); yield break; }
                    string status = (string)ReadJson(write)?["error"]?["status"];
                    if (status != "FAILED_PRECONDITION" && status != "ALREADY_EXISTS" &&
                        status != "ABORTED" && write.responseCode != 412) break;
                }
            }
            completed(PublishResult.Unavailable);
        }

        private static bool TryScore(JToken fields, out int score) =>
            int.TryParse((string)fields?["score"]?["integerValue"], NumberStyles.None, CultureInfo.InvariantCulture, out score)
            && score >= 0 && score <= 10000;

        private static JObject ReadJson(UnityWebRequest request)
        {
            try
            {
                using (var reader = new JsonTextReader(new StringReader(request.downloadHandler?.text ?? ""))
                    { DateParseHandling = DateParseHandling.None })
                    return JObject.Load(reader);
            }
            catch (JsonException) { return null; }
        }
    }
}
