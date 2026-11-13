using System;
using System.Globalization;
using UnityEngine;

namespace RhythmRehab
{
    // USB wire format: IMU,GAIT-SHANK,sequence,w,x,y,z followed by a newline.
    public readonly struct ImuSerialPacket
    {
        public readonly ushort sequence;
        public readonly Quaternion orientation;
        public ImuSerialPacket(ushort sequence, Quaternion orientation)
        { this.sequence = sequence; this.orientation = orientation; }

        public Vector3 Gravity
        {
            get
            {
                var q = orientation;
                return new Vector3(2 * (q.x * q.z - q.w * q.y),
                    2 * (q.w * q.x + q.y * q.z),
                    q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z).normalized;
            }
        }

        public static bool TryParse(string line, out ImuSerialPacket packet)
        {
            packet = default;
            if (string.IsNullOrWhiteSpace(line) || line.Length > 160) return false;
            var fields = line.Trim().Split(',');
            if (fields.Length != 7 || fields[0] != "IMU" || fields[1] != "GAIT-SHANK" ||
                !ushort.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)) return false;
            var values = new float[4];
            for (int i = 0; i < values.Length; i++)
                if (!float.TryParse(fields[i + 3], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]) ||
                    float.IsNaN(values[i]) || float.IsInfinity(values[i])) return false;
            float norm = 0;
            foreach (var value in values) norm += value * value;
            if (norm < .5f || norm > 1.5f) return false;
            float scale = 1 / Mathf.Sqrt(norm);
            packet = new ImuSerialPacket(sequence, new Quaternion(values[1] * scale, values[2] * scale, values[3] * scale, values[0] * scale));
            return true;
        }
    }
}
