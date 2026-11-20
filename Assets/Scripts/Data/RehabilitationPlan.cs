using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RhythmRehab
{
    [Serializable]
    public sealed class RehabilitationPlan
    {
        public const string GameId = "GAME003";
        public const string ExerciseType = "leg-raise";
        public string id, patientId, name, affectedSide;
        public float minimumDegrees, maximumDegrees;
        public int repetitions, sets;
        public bool IsTest => id != null && id.StartsWith("TEST_", StringComparison.Ordinal);
        public float TargetDegrees(HeightLevel level) => Mathf.Lerp(minimumDegrees, maximumDegrees, (int)level / 2f);

        public float GameplayHeight(float degrees)
        {
            // The existing guide lines stay in place while their physical angles come from the plan.
            return ChartNote.LowTargetHeight + (1 - ChartNote.LowTargetHeight) *
                (degrees - minimumDegrees) / (maximumDegrees - minimumDegrees);
        }

        public float PracticeDegrees(float height)
        {
            float low = ChartNote.LowTargetHeight;
            return height < low
                ? Mathf.Lerp(0, minimumDegrees, height / low)
                : Mathf.Lerp(minimumDegrees, maximumDegrees, Mathf.InverseLerp(low, 1, height));
        }

        internal static RehabilitationPlan Parse(JObject fields, string patient, DateTimeOffset now)
        {
            string Value(string field) => (string)fields?[field]?["stringValue"];
            bool Date(string field, out DateTimeOffset result) =>
                DateTimeOffset.TryParse((string)fields?[field]?["timestampValue"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result);
            if (Value("patientID") != patient || Value("gameID") != GameId || Value("exerciseType") != ExerciseType ||
                !string.Equals(Value("planStatus"), "Active", StringComparison.OrdinalIgnoreCase) ||
                !Date("startDate", out var start) || !Date("endDate", out var end) || now < start || now > end)
                return null;

            var placements = fields["sensorPlacements"]?["arrayValue"]?["values"] as JArray;
            if (placements == null || placements.Count != 1 ||
                !string.Equals((string)placements[0]["stringValue"], "SHANK", StringComparison.OrdinalIgnoreCase))
                return null;

            var range = fields["targetROM"]?["mapValue"]?["fields"];
            if (!float.TryParse(Number(range?["minimum"]), NumberStyles.Float, CultureInfo.InvariantCulture, out var min) ||
                !float.TryParse(Number(range?["maximum"]), NumberStyles.Float, CultureInfo.InvariantCulture, out var max) ||
                !(min > 0 && max > min && max <= 150) ||
                !int.TryParse(Number(fields["targetRepetition"]), out var reps) || reps <= 0 ||
                !int.TryParse(Number(fields["targetSet"]), out var sets) || sets <= 0 ||
                (long)reps * sets > int.MaxValue || string.IsNullOrWhiteSpace(Value("planID")))
                return null;

            return new RehabilitationPlan
            {
                id = Value("planID"), patientId = patient, name = PatientContext.CleanName(Value("plan")),
                affectedSide = Value("affectedSide"), minimumDegrees = min, maximumDegrees = max,
                repetitions = reps, sets = sets
            };
        }

        private static string Number(JToken field) => (string)(field?["doubleValue"] ?? field?["integerValue"]);
    }
}
