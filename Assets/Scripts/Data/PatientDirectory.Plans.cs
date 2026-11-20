using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RhythmRehab
{
    public sealed partial class PatientDirectory
    {
        private const string LegRaiseAlgorithmVersion = "rhythmrehab-shin-tilt-leg-raise-v1";

        public IEnumerator ReadPlans(string patientId, Action<List<RehabilitationPlan>, string> completed)
        {
            string token = null;
            yield return GetPatientToken(patientId, value => token = value);
            if (token == null) { completed(null, "Sign in and choose a patient to load plans."); yield break; }

            var query = new JObject
            {
                ["structuredQuery"] = new JObject
                {
                    ["from"] = new JArray(new JObject { ["collectionId"] = "rehabilitationPlans" }),
                    ["where"] = new JObject
                    {
                        ["fieldFilter"] = new JObject
                        {
                            ["field"] = new JObject { ["fieldPath"] = "patientID" },
                            ["op"] = "EQUAL", ["value"] = StringField(patientId)
                        }
                    }
                }
            };
            using var request = JsonRequest(DocumentsUrl + ":runQuery", query, token);
            yield return request.SendWebRequest();
            if (!CanAccessPatient(patientId))
            { completed(null, "The selected patient changed. Reopen this page to load their plans."); yield break; }
            if (request.result != UnityWebRequest.Result.Success)
            { completed(null, "Could not load assigned plans. Reopen this page to retry."); yield break; }

            List<RehabilitationPlan> plans;
            try
            {
                plans = JArray.Parse(request.downloadHandler.text)
                    .Select(row => RehabilitationPlan.Parse(row["document"]?["fields"] as JObject, patientId, DateTimeOffset.UtcNow))
                    .Where(plan => plan != null).OrderBy(plan => plan.name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(plan => plan.id, StringComparer.Ordinal).ToList();
            }
            catch (JsonException) { completed(null, "The assigned plan response could not be read."); yield break; }
            completed(plans, null);
        }

        public IEnumerator UploadPlanSession(SessionSummary summary, Action<string> completed)
        {
            var plan = summary.plan;
            if (!summary.CanUploadPlan ||
                summary.patientId != plan.patientId || !Guid.TryParseExact(summary.sessionId, "N", out _) ||
                !DateTimeOffset.TryParse(summary.startedUtc, out var start) || !DateTimeOffset.TryParse(summary.endedUtc, out var end))
            { completed("This record is not an uploadable plan session."); yield break; }
            string token = null;
            yield return GetPatientToken(summary.patientId, value => token = value);
            if (token == null) { completed("Sign in and select the session’s patient before uploading."); yield break; }

            int scheduledIndex = 0;
            yield return ReadNextScheduledIndex(plan, token, index => scheduledIndex = index);
            if (scheduledIndex == 0 || !CanAccessPatient(summary.patientId))
            {
                completed("Could not verify the next session number. Your session is saved locally. Try uploading again.");
                yield break;
            }

            string collection = DocumentsUrl + "/rehabilitationSessions";
            var body = new JObject { ["fields"] = SessionFields(summary, start, end, scheduledIndex) };
            using var request = JsonRequest(collection + "?documentId=" + summary.sessionId, body, token);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success) { completed(null); yield break; }

            // A retry after a timeout can find the original document. Never overwrite it.
            if (request.responseCode == 409 && CanAccessPatient(summary.patientId))
            {
                using var existing = UnityWebRequest.Get(collection + "/" + summary.sessionId);
                existing.SetRequestHeader("Authorization", "Bearer " + token);
                existing.timeout = 20;
                yield return existing.SendWebRequest();
                if (existing.result == UnityWebRequest.Result.Success)
                {
                    JToken fields = null;
                    try { fields = JObject.Parse(existing.downloadHandler.text)["fields"]; }
                    catch (JsonException) { /* Retry remains available if the existing document could not be read. */ }
                    if (Text(fields, "sessionID") == summary.sessionId && Text(fields, "patientID") == summary.patientId &&
                        Text(fields, "planID") == plan.id && Text(fields, "gameID") == RehabilitationPlan.GameId)
                    { completed(null); yield break; }
                }
            }
            completed(request.responseCode == 403 ? "Firebase denied this upload. Check the account’s plan permissions." :
                "Upload could not be confirmed. The session is still saved here and can be retried.");
        }

        private IEnumerator ReadNextScheduledIndex(RehabilitationPlan plan, string token, Action<int> completed)
        {
            var query = new JObject
            {
                ["structuredQuery"] = new JObject
                {
                    ["from"] = new JArray(new JObject { ["collectionId"] = "rehabilitationSessions" }),
                    ["select"] = new JObject
                    {
                        ["fields"] = new JArray(new[] { "patientID", "planID", "sessionStatus" }
                            .Select(field => new JObject { ["fieldPath"] = field }))
                    },
                    ["where"] = new JObject
                    {
                        ["compositeFilter"] = new JObject
                        {
                            ["op"] = "AND",
                            ["filters"] = new JArray(MatchString("patientID", plan.patientId), MatchString("planID", plan.id))
                        }
                    }
                }
            };
            using var request = JsonRequest(DocumentsUrl + ":runQuery", query, token);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success || !CanAccessPatient(plan.patientId)) yield break;

            int index;
            try { index = NextScheduledIndex(JArray.Parse(request.downloadHandler.text), plan); }
            catch (JsonException) { yield break; }
            completed(index);
        }

        private static int NextScheduledIndex(JArray sessions, RehabilitationPlan plan)
        {
            int completed = 0;
            foreach (var row in sessions)
            {
                var fields = row["document"]?["fields"];
                if (Text(fields, "patientID") == plan.patientId && Text(fields, "planID") == plan.id &&
                    Text(fields, "sessionStatus") == "Completed")
                    completed++;
            }
            return completed + 1;
        }

        private static JObject MatchString(string field, string value) => new JObject
        {
            ["fieldFilter"] = new JObject
            {
                ["field"] = new JObject { ["fieldPath"] = field },
                ["op"] = "EQUAL",
                ["value"] = StringField(value)
            }
        };

        private bool CanAccessPatient(string patientId) =>
            login && login.IsSignedIn && patientId == PatientContext.PatientId && patients.Any(p => p.id == patientId);

        private IEnumerator GetPatientToken(string patientId, Action<string> completed)
        {
            if (!CanAccessPatient(patientId)) { completed(null); yield break; }
            string token = null;
            yield return login.GetToken(value => token = value);
            completed(CanAccessPatient(patientId) ? token : null);
        }

        private static UnityWebRequest JsonRequest(string url, JObject body, string token)
        {
            var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString(Formatting.None))),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 20
            };
            request.SetRequestHeader("Authorization", "Bearer " + token);
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private static JObject SessionFields(SessionSummary session, DateTimeOffset start, DateTimeOffset end, int scheduledIndex)
        {
            var plan = session.plan;
            var now = new JObject { ["timestampValue"] = DateTime.UtcNow.ToString("O") };
            var side = OptionalStringField(plan.affectedSide);
            var range = MapField(new JObject
            {
                ["minimum"] = DegreesField(plan.minimumDegrees),
                ["maximum"] = DegreesField(plan.maximumDegrees)
            });
            var placements = new JObject
            {
                ["arrayValue"] = new JObject { ["values"] = new JArray(StringField("SHANK")) }
            };
            return new JObject
            {
                ["affectedSide"] = side,
                ["algorithmVersion"] = StringField(LegRaiseAlgorithmVersion),
                ["alignmentCoverage"] = NullField(),
                ["angleUnit"] = StringField("degree"),
                ["averageDirectionAccuracy"] = NullField(),
                ["averageHoldDuration"] = NullField(),
                ["averageMovementAccuracy"] = NullField(),
                ["averageROM"] = NullField(),
                ["averageRotationAngle"] = NullField(),
                ["averageSquatDepth"] = NullField(),
                ["averageTargetHitAccuracy"] = NullField(),
                ["bodySide"] = side.DeepClone(),
                ["calibrationMethod"] = StringField("rest-and-raised-shin-reference"),
                ["completionRate"] = DoubleField(Math.Round(100d * Math.Min(1d, session.successfulLegRaises / ((double)plan.repetitions * plan.sets)), 2)),
                ["createdAt"] = now,
                ["date"] = StringField(end.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ["dateTimeZone"] = StringField("UTC"),
                ["duration"] = DoubleField(session.duration),
                ["durationSeconds"] = DoubleField(session.duration),
                ["durationUnit"] = StringField("second"),
                ["endTime"] = IntField(end.ToUnixTimeMilliseconds()),
                ["exerciseId"] = StringField(RehabilitationPlan.ExerciseType),
                ["exerciseName"] = StringField("Leg Raise"),
                ["exerciseType"] = StringField(RehabilitationPlan.ExerciseType),
                ["finalScore"] = session.HasAccuracy ? DoubleField(Math.Round(100 * session.Accuracy, 2)) : NullField(),
                ["gameID"] = StringField(RehabilitationPlan.GameId),
                ["limbType"] = StringField("lower-limb"),
                ["maxROM"] = NullField(),
                ["minROM"] = NullField(),
                ["orientationConvention"] = StringField("Hamilton quaternion wxyz; signed shin tilt relative to rest"),
                ["painLevelAfter"] = NullField(),
                ["painLevelBefore"] = NullField(),
                ["patientID"] = StringField(session.patientId),
                ["patientId"] = StringField(session.patientId),
                ["pausedDurationSeconds"] = NullField(),
                ["planID"] = StringField(plan.id),
                ["planTargetSnapshot"] = MapField(new JObject
                {
                    ["affectedSide"] = side.DeepClone(),
                    ["exerciseType"] = StringField(RehabilitationPlan.ExerciseType),
                    ["gameID"] = StringField(RehabilitationPlan.GameId),
                    ["planID"] = StringField(plan.id),
                    ["requiredSensorPlacements"] = placements.DeepClone(),
                    ["targetRepetition"] = IntField(plan.repetitions),
                    ["targetROM"] = range.DeepClone(),
                    ["targetSet"] = IntField(plan.sets)
                }),
                ["processedAt"] = NullField(),
                ["processingError"] = NullField(),
                ["processingSampleCount"] = NullField(),
                ["processingStatus"] = NullField(),
                ["qualityFlags"] = new JObject { ["arrayValue"] = new JObject { ["values"] = new JArray() } },
                ["rawCaptureVersion"] = NullField(),
                ["requiredSensorPlacements"] = placements,
                ["saveStatus"] = StringField("saved"),
                ["scheduledSessionIndex"] = IntField(scheduledIndex),
                ["schemaVersion"] = IntField(3),
                ["sensorCount"] = IntField(1),
                ["sessionID"] = StringField(session.sessionId),
                ["sessionMode"] = StringField("plan"),
                ["sessionStatus"] = StringField(session.endedEarly ? "EndedEarly" : "Completed"),
                ["startTime"] = IntField(start.ToUnixTimeMilliseconds()),
                ["stopReason"] = StringField(session.endedEarly ? "user-stopped" : "completed"),
                ["successRepetitions"] = IntField(session.successfulLegRaises),
                ["targetRepetitions"] = IntField(plan.repetitions),
                ["targetROM"] = range,
                ["targetSets"] = IntField(plan.sets),
                ["timestampSource"] = StringField("unity-session-clock"),
                ["totalFrames"] = NullField(),
                ["totalRepetitions"] = IntField(session.legRaises),
                ["updatedAt"] = now.DeepClone()
            };
        }

        private static JObject NullField() => new JObject { ["nullValue"] = JValue.CreateNull() };
        private static JObject OptionalStringField(string value) => string.IsNullOrWhiteSpace(value) ? NullField() : StringField(value);
        private static JObject DegreesField(float value) => value == Math.Floor(value) ? IntField((long)value) : DoubleField(value);
        private static JObject StringField(string value) => new JObject { ["stringValue"] = value };
        private static JObject IntField(long value) => new JObject { ["integerValue"] = value.ToString(CultureInfo.InvariantCulture) };
        private static JObject DoubleField(double value) => new JObject { ["doubleValue"] = value };
        private static JObject BoolField(bool value) => new JObject { ["booleanValue"] = value };
        private static JObject MapField(JObject fields) => new JObject { ["mapValue"] = new JObject { ["fields"] = fields } };
    }
}
