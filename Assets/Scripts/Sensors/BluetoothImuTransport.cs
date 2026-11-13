using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace RhythmRehab
{
    public sealed class BluetoothImuTransport : IImuTransport
    {
        private readonly object sync = new object();
        private Process bridge;
        private bool running, open;
        private string error, status = "Starting Bluetooth scan…";
        private ImuSerialPacket latest;
        private long version;
        private double receivedAt;

        public bool IsOpen { get { lock (sync) return open; } }
        public bool IsConnecting { get { lock (sync) return running && !open; } }
        public string Error { get { lock (sync) return error; } }
        public string Status { get { lock (sync) return status; } }

        public void Start()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "RhythmRehabBluetooth.exe");
            if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer)
                throw new PlatformNotSupportedException("Sensor Bluetooth support requires Windows.");
            if (!File.Exists(path)) throw new FileNotFoundException("RhythmRehabBluetooth.exe is missing from StreamingAssets.");
            bridge = new Process
            {
                StartInfo = new ProcessStartInfo(path)
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardInput = true
                }
            };
            bridge.OutputDataReceived += ReadLine;
            running = true;
            bridge.Start();
            bridge.BeginOutputReadLine();
        }

        private void ReadLine(object sender, DataReceivedEventArgs args)
        {
            lock (sync)
            {
                if (!running) return;
                string line = args.Data;
                if (line == null)
                {
                    open = running = false;
                    if (error == null) error = "The Windows Bluetooth receiver closed.";
                }
                else if (line == "READY") open = true;
                else if (line.StartsWith("ERROR ", StringComparison.Ordinal)) error = line.Substring(6);
                else if (line.StartsWith("STATUS ", StringComparison.Ordinal)) status = line.Substring(7);
                else if (ImuSerialPacket.TryParse(line, out var packet) && (version == 0 || packet.sequence != latest.sequence))
                {
                    latest = packet;
                    receivedAt = UsbImuTransport.Now;
                    version++;
                }
            }
        }

        public bool Snapshot(out ImuSerialPacket packet, out long packetVersion, out double age)
        {
            lock (sync)
            {
                packet = latest;
                packetVersion = version;
                age = UsbImuTransport.Now - receivedAt;
                return open && version > 0;
            }
        }

        public void Dispose()
        {
            lock (sync) open = running = false;
            if (bridge == null) return;
            try
            {
                if (!bridge.HasExited)
                {
                    bridge.StandardInput.Close();
                    if (!bridge.WaitForExit(250)) bridge.Kill();
                }
            }
            catch (InvalidOperationException) { }
            finally { bridge.Dispose(); bridge = null; }
        }
    }
}
