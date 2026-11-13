using TMPro;
using UnityEngine;
namespace RhythmRehab
{
    public sealed class SensorFrameworkPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text status;
        [SerializeField] private bool calibrationPage;
        [SerializeField] private TMP_Text state, details, portLabel, connectLabel, calibrateLabel, liveHeight;
        [SerializeField] private UnityEngine.UI.Button connectButton, calibrateButton, useSensorButton;
        private float refreshAt;
        private UsbSensorConnection Sensor => UsbSensorConnection.Instance;
        private void Start() => Refresh();
        private void Update()
        {
            if (Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + .1f; Refresh();
        }
        public void Connect()
        {
            if (!Sensor) return;
            if (Sensor.IsOpen || Sensor.IsConnecting) Sensor.Disconnect(); else Sensor.ConnectBluetooth();
            Refresh();
        }
        public void ExplainCalibration() { if (Sensor) Sensor.CaptureNext(); Refresh(); }
        public void UseSensor() { if (Sensor) Sensor.UseSensor(); Refresh(); }
        public void UseMouse() { if (Sensor) Sensor.UseMouse(); Refresh(); }
        private void Refresh()
        {
            var sensor = Sensor;
            if (!sensor) { if (status) status.text = "Sensor service is unavailable. Reopen this scene."; return; }
            bool fresh = sensor.HasFreshReading, ready = sensor.Calibration.IsReady;
            if (status) status.text = sensor.Message;
            if (portLabel) portLabel.text = "GAIT-SHANK";
            if (connectLabel) connectLabel.text = sensor.IsOpen ? "DISCONNECT" : sensor.IsConnecting ? "CANCEL" : "CONNECT BLUETOOTH";
            if (connectButton) connectButton.interactable = true;
            if (calibrateButton) calibrateButton.interactable = fresh && !sensor.IsCapturing;
            if (useSensorButton) useSensorButton.interactable = fresh && ready && !sensor.IsCapturing;
            if (calibrateLabel) calibrateLabel.text = sensor.IsCapturing ? "CAPTURING…" : ready ? "RECALIBRATE" : sensor.Calibration.HasRest ? "CAPTURE RAISED" : "CAPTURE REST";
            if (state)
            {
                state.text = calibrationPage ? !fresh ? "WAITING\nFOR SENSOR" : ready ? "READY" : sensor.Calibration.HasRest ? "02  RAISE" : "01  REST" :
                    fresh ? "RECEIVING DATA" : sensor.IsOpen ? "WAITING FOR DATA" : sensor.IsConnecting ? "CONNECTING" : "NOT CONNECTED";
                state.color = fresh ? new Color(.3f, 1, .5f) : new Color(.7f, .75f, .85f);
            }
            if (details) details.text = calibrationPage ? ready ? $"Reference captured\n{(sensor.InputEnabled ? "Sensor control active" : "Calibration ready")}" :
                sensor.Calibration.HasRest ? "Lift your thigh with your knee bent.\nKeep the leg in an L shape.\nHold still, then capture." : "Sit with your knee bent and your foot resting.\nKeep the shin sensor still.\nCapture your resting position." :
                $"Connection     Bluetooth LE\nDevice     {(fresh ? "GAIT-SHANK" : "—")}\nReadings     {sensor.PacketVersion}\nControl     {(sensor.InputEnabled ? "SENSOR AUTO HIT" : "MOUSE + KEYBOARD")}";
            if (liveHeight) liveHeight.text = ready
                ? $"<size=21>SENSOR TILT</size>\n<size=45>{(fresh ? sensor.MovementDegrees.ToString("0.0") + "°" : "—")}</size>\n<size=20>FROM REST\n\nREFERENCE RANGE\n{sensor.Calibration.RangeDegrees:0.0}°</size>"
                : "<size=21>SENSOR TILT</size>\n<size=45>—</size>\n<size=20>Capture rest\nand raised positions</size>";
        }
    }
}
