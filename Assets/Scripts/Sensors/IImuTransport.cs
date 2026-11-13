using System;

namespace RhythmRehab
{
    public interface IImuTransport : IDisposable
    {
        bool IsOpen { get; }
        string Error { get; }
        void Start();
        bool Snapshot(out ImuSerialPacket packet, out long packetVersion, out double age);
    }
}
