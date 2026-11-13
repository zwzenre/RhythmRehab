using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using RhythmRehab;
using UnityEngine.Networking;

public static class FirestoreSessionSmokeTest
{
    private const string Collection =
        "https://firestore.googleapis.com/v1/projects/patient-dashboard-c109b/databases/(default)/documents/rhythmRehabUploadTests";
    private const string ReportPath = "Temp/FirestoreSessionSmokeTest.txt";
    private static bool running;

    public static string Run()
    {
        if (running) return "An upload test is already running.";
        var end = DateTimeOffset.UtcNow;
        var start = end.AddSeconds(-24.5);
        var session = new SessionSummary
        {
            songTitle = "Synthetic RhythmRehab test",
            duration = 24.5f,
            startedUtc = start.ToString("O"),
            endedUtc = end.ToString("O"),
            totalNotes = 4
        };
        session.Record(HitJudgement.Perfect);
        session.Record(HitJudgement.Great);
        session.Record(HitJudgement.Good);
        session.Record(HitJudgement.Miss);

        double finalScore = Math.Round(session.Accuracy * 100, 2);
        var fields = new JObject
        {
            ["isTest"] = new JObject { ["booleanValue"] = true },
            ["gameID"] = Text("GAME003"),
            ["sessionID"] = Text(session.sessionId),
            ["sessionMode"] = Text("standalone"),
            ["songTitle"] = Text(session.songTitle),
            ["startTime"] = Integer(start.ToUnixTimeMilliseconds()),
            ["endTime"] = Integer(end.ToUnixTimeMilliseconds()),
            ["durationSeconds"] = Number(session.duration),
            ["finalScore"] = Number(finalScore),
            ["completionRate"] = Number(Math.Round(session.Completion * 100, 2))
        };
        string path = Collection + "/" + session.sessionId;
        var body = new JObject { ["fields"] = fields };
        Report("Pending: " + path);
        running = true;
        Send(new UnityWebRequest(Collection + "?documentId=" + session.sessionId, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString()))
            {
                contentType = "application/json"
            },
            downloadHandler = new DownloadHandlerBuffer()
        }, create =>
        {
            if (create.result != UnityWebRequest.Result.Success)
            {
                Finish("Create failed", create);
                return;
            }
            long createStatus = create.responseCode;
            Send(UnityWebRequest.Get(path), read =>
            {
                if (read.result != UnityWebRequest.Result.Success)
                {
                    Finish("Create succeeded, read-back failed", read);
                    return;
                }
                var stored = JObject.Parse(read.downloadHandler.text)["fields"];
                bool matches = (string)stored?["sessionID"]?["stringValue"] == session.sessionId
                    && (string)stored?["gameID"]?["stringValue"] == "GAME003"
                    && (double?)stored?["finalScore"]?["doubleValue"] == finalScore
                    && stored?["rhythmScore"] == null
                    && stored?["rhythmAccuracyPercent"] == null
                    && (double?)stored?["durationSeconds"]?["doubleValue"] == session.duration;
                running = false;
                Report($"{(matches ? "PASS" : "Read-back values did not match")}. Create HTTP {createStatus}; read HTTP {read.responseCode}. {path}");
            });
        });
        return "Started Unity Firestore smoke test. " + path;
    }

    private static JObject Text(string value) => new JObject { ["stringValue"] = value };
    private static JObject Integer(long value) => new JObject { ["integerValue"] = value.ToString(CultureInfo.InvariantCulture) };
    private static JObject Number(double value) => new JObject { ["doubleValue"] = value };

    private static void Send(UnityWebRequest request, Action<UnityWebRequest> complete)
    {
        try
        {
            request.timeout = 15;
            request.SendWebRequest().completed += _ =>
            {
                using (request)
                {
                    try { complete(request); }
                    catch (Exception error) { running = false; Report(error.Message); }
                }
            };
        }
        catch (Exception error)
        {
            request.Dispose();
            running = false;
            Report(error.Message);
        }
    }

    private static void Finish(string label, UnityWebRequest request)
    {
        running = false;
        Report($"{label}. HTTP {request.responseCode}. {request.error}\n{request.downloadHandler.text}");
    }

    private static void Report(string text) => File.WriteAllText(ReportPath, text);
}
