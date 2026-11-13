using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace RhythmRehab
{
    // Only this worker touches the COM port. Unity reads a locked snapshot on its main thread.
    public sealed class UsbImuTransport : IImuTransport
    {
        private readonly object sync = new object();
        private readonly string portName;
        private Thread worker;
        private volatile bool running;
        private bool open;
        private ImuSerialPacket latest;
        private long version;
        private double receivedAt;
        private string error;
        public static double Now => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        public UsbImuTransport(string portName) { this.portName = portName; }
        public bool IsOpen { get { lock (sync) return open; } }
        public string Error { get { lock (sync) return error; } }
        public static string[] Ports()
        {
            var ports = SerialPort.GetPortNames(); Array.Sort(ports, StringComparer.OrdinalIgnoreCase); return ports;
        }
        public void Start()
        {
            if (worker != null) throw new InvalidOperationException("The serial worker has already started.");
            running = true;
            worker = new Thread(Read) { IsBackground = true, Name = "RhythmRehab USB IMU" };
            worker.Start();
        }
        public bool Snapshot(out ImuSerialPacket packet, out long packetVersion, out double age)
        {
            lock (sync) { packet = latest; packetVersion = version; age = Now - receivedAt; return open && version > 0; }
        }
        private void Read()
        {
            try
            {
                using (var port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One))
                {
                    port.Handshake = Handshake.None; port.DtrEnable = true; port.RtsEnable = false;
                    port.ReadTimeout = 100; port.WriteTimeout = 100; port.NewLine = "\n";
                    port.Open(); lock (sync) open = true;
                    // Bound partial lines so noise or the wrong firmware cannot grow a buffer forever.
                    var line = new System.Text.StringBuilder(160); bool overflow = false;
                    while (running)
                    {
                        int ch;
                        try { ch = port.ReadChar(); } catch (TimeoutException) { continue; }
                        if (ch < 0) continue;
                        if (ch == '\n')
                        {
                            if (!overflow && ImuSerialPacket.TryParse(line.ToString(), out var sample))
                                lock (sync)
                                {
                                    if (version == 0 || sample.sequence != latest.sequence)
                                    { latest = sample; receivedAt = Now; version++; }
                                }
                            line.Clear(); overflow = false;
                        }
                        else if (ch != '\r') { if (line.Length < 160 && !overflow) line.Append((char)ch); else overflow = true; }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is InvalidOperationException)
            { lock (sync) error = ex.Message; }
            finally { lock (sync) open = false; running = false; }
        }
        public void Dispose()
        {
            running = false;
            if (worker != null && worker.IsAlive && !worker.Join(1500))
                throw new IOException("The COM port is still closing. Wait before connecting again.");
        }
    }
}
