using System;
using System.Collections;
using UnityEngine;

namespace RhythmRehab
{
    // Authored on the shared SceneTransition prefab; the same connection survives scene changes.
    public sealed class UsbSensorConnection : MonoBehaviour
    {
        [SerializeField, Min(.05f)] private float staleAfterSeconds = .5f;
        [SerializeField, Min(0)] private float smoothingSeconds = .08f;
        public static UsbSensorConnection Instance { get; private set; }
        public ShinHeightCalibration Calibration { get; } = new ShinHeightCalibration();
        private IImuTransport transport;
        private ImuSerialPacket sample;
        private long version;
        private float height;
        private bool fresh, capturing, awaitingFirstReading;
        private bool preferMouse;
        private bool sensorSelected;
        public bool SensorControlSelected => sensorSelected && !preferMouse;
        private string message = "Connect your Bluetooth shin sensor to begin.";
        public string PortName { get; private set; } = "";
        public bool IsOpen => transport != null && transport.IsOpen;
        public bool IsConnecting => transport is BluetoothImuTransport bluetooth && bluetooth.IsConnecting;
        public bool HasFreshReading => fresh && transport != null && transport.Snapshot(out _, out _, out var age) && age <= staleAfterSeconds;
        public bool InputEnabled { get; private set; }
        public bool IsCapturing => capturing;
        public long PacketVersion => version;
        public float Height => height;
        public float MovementDegrees => Calibration.MovementDegrees(sample.Gravity);
        public string Message
        {
            get
            {
                if (transport?.Error != null) return transport.Error;
                if (transport is BluetoothImuTransport bluetooth && bluetooth.IsConnecting) return bluetooth.Status;
                if (IsOpen && !HasFreshReading)
                    return version > 0 ? "Sensor readings interrupted. Check the connection." : "Connected. Waiting for GAIT-SHANK readings…";
                return message;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => Instance = null;
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }
        private void Update()
        {
            fresh = transport != null && transport.Snapshot(out var next, out var nextVersion, out var age) && age <= staleAfterSeconds;
            if (!fresh) return;
            transport.Snapshot(out next, out nextVersion, out _); sample = next; version = nextVersion;
            if (awaitingFirstReading)
            {
                awaitingFirstReading = false;
                message = "Receiving live GAIT-SHANK readings. Open SENSOR CALIBRATION to set your movement range.";
            }
            if (Calibration.IsReady)
                height = Mathf.Lerp(height, Calibration.Height(sample.Gravity), smoothingSeconds <= 0 ? 1 : 1 - Mathf.Exp(-Time.unscaledDeltaTime / smoothingSeconds));
        }
        public string[] GetPorts()
        {
            try { return UsbImuTransport.Ports(); }
            catch (Exception ex) { message = "Could not list COM ports: " + ex.Message; return Array.Empty<string>(); }
        }
        public void Connect(string port)
        {
            if (string.IsNullOrWhiteSpace(port)) { message = "Choose the board's COM port first."; return; }
            StartConnection(new UsbImuTransport(port), port, "Opening " + port + " at 115200 baud…");
        }
        public void ConnectBluetooth()
        {
            StartConnection(new BluetoothImuTransport(), "GAIT-SHANK", "Starting Bluetooth scan…");
        }
        private void StartConnection(IImuTransport next, string deviceName, string connectingMessage)
        {
            Disconnect();
            if (transport != null) return;
            PortName = deviceName; version = 0; height = 0; awaitingFirstReading = true;
            transport = next;
            try { transport.Start(); message = connectingMessage; }
            catch (Exception error) { Disconnect(); message = error.Message; }
        }
        public void Disconnect()
        {
            StopAllCoroutines(); capturing = false; InputEnabled = false; fresh = false; awaitingFirstReading = false;
            if (transport != null)
            {
                try { transport.Dispose(); transport = null; }
                catch (Exception ex) { message = ex.Message; return; }
            }
            Calibration.Reset(); message = "Disconnected. Mouse control is available.";
        }
        public bool TryGetHeight(out float result) { result = height; return InputEnabled && Calibration.IsReady && HasFreshReading; }
        public void UseMouse()
        {
            preferMouse = true;
            InputEnabled = false;
            message = "Mouse control selected.";
        }
        public void PrepareGameplayInput(float minimumRange = 0)
        {
            InputEnabled = false;
            if (!preferMouse && !capturing && Calibration.IsReady && HasFreshReading &&
                Calibration.RangeDegrees >= minimumRange) UseSensor();
        }
        public void UseSensor()
        {
            if (!Calibration.IsReady || !HasFreshReading) { message = "Connect and capture both positions before using the sensor."; return; }
            preferMouse = false;
            sensorSelected = true;
            InputEnabled = true; message = "Sensor control enabled. Gameplay judges notes automatically when you match the target height.";
        }
        public void CaptureNext()
        {
            if (capturing) return;
            if (!HasFreshReading) { message = "No fresh GAIT-SHANK readings. Check the sensor connection."; return; }
            StartCoroutine(Capture(!Calibration.HasRest || Calibration.IsReady));
        }
        private IEnumerator Capture(bool rest)
        {
            capturing = true; InputEnabled = false;
            message = "Keep the shin still. Capturing " + (rest ? "resting" : "raised") + " position…";
            float end = Time.realtimeSinceStartup + 1.25f; long previous = -1; int count = 0;
            Vector3 sum = Vector3.zero;
            var samples = new System.Collections.Generic.List<Vector3>();
            while (Time.realtimeSinceStartup < end)
            {
                if (!HasFreshReading) { message = "Capture stopped: sensor data was interrupted."; capturing = false; yield break; }
                if (version != previous) { var gravity = sample.Gravity; sum += gravity; samples.Add(gravity); count++; previous = version; }
                yield return null;
            }
            capturing = false;
            if (count < 15 || sum.sqrMagnitude < .1f) { message = "Too few readings. Check the connection and try again."; yield break; }
            var mean = sum.normalized;
            foreach (var value in samples)
                if (Vector3.Angle(mean, value) > 4) { message = "The sensor moved during capture. Keep still and try again."; yield break; }
            if (rest) { Calibration.CaptureRest(mean); message = "Rest captured. Lift your thigh with your knee bent, hold still, then capture raised."; }
            else if (Calibration.CaptureRaised(mean, out var error))
            {
                height = Calibration.Height(sample.Gravity);
                message = preferMouse ? "Both positions captured. Select USE SENSOR to switch from mouse controls." :
                    "Both positions captured. Gameplay will use the sensor while readings are live.";
            }
            else message = error;
        }
        private void OnApplicationQuit() { if (Instance == this) Disconnect(); }
        private void OnDestroy() { if (Instance == this) { Disconnect(); Instance = null; } }
    }
}
