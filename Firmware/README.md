# Bluetooth shin sensor setup

`MahonyGaitImuV2/MahonyGaitImuV2.ino` is the sensor team's original sketch. For the shin-mounted RhythmRehab setup, select `SEG_SHANK` in its `#define SEGMENT` line when flashing the board. The supplied copy defaults to `SEG_FOOT`.

On Windows, turn on Bluetooth, power on one `GAIT-SHANK` sensor, and select **Connect Bluetooth** in RhythmRehab's Sensor Connection screen. Unity uses the bundled `RhythmRehabBluetooth.exe` to scan and subscribe to the sketch's BLE characteristic. After readings arrive, use the existing Sensor Calibration screen. The receiver decodes the 18-byte packet as a little-endian `uint16` followed by four `float32` values in `w, x, y, z` order. See [Microsoft's Windows BLE GATT documentation](https://learn.microsoft.com/en-us/windows/apps/develop/devices-sensors/gatt-client).

Build the receiver from source with `Tools/BuildBluetoothImuBridge.ps1` using the Windows SDK, .NET Framework 4.7.2 reference assemblies, and a .NET SDK. Run the resulting executable with `--self-test` to check packet decoding without a sensor.

## Legacy USB bench setup

The USB notes below describe the earlier bench setup. The in-game connection screen now uses Bluetooth.

The supplied original firmware sends an 18-byte quaternion notification over BLE.
Its USB output contains startup text only, and its sampling loop waits for a BLE
central. Plugging in USB alone therefore does not supply movement data to Unity.

`RhythmRehabUsbImu/RhythmRehabUsbImu.ino` is a separate USB-only adaptation for the
Seeed XIAO nRF52840 Sense and the LSM6DS3 library used by the original sketch. It
keeps the measured-dt Mahony filter, startup gyro-bias calibration and stationary
bias tracking. BLE is not used in this version. Preserve the original sketch if
another application still needs BLE.

## Upload and connect

1. Open `RhythmRehabUsbImu/RhythmRehabUsbImu.ino` in Arduino IDE.
2. Select **Seeed nRF52 mbed-enabled Boards → XIAO nRF52840 Sense (No Updates)**,
   matching the user's current board selection, and the board's COM port.
3. Upload. Keep the board still during its approximately 2.5-second power-on gyro
   calibration. The sketch was compile-checked against the installed Seeed mbed
   2.9.3 board package. It has not been uploaded or hardware-verified by Codex.
4. Open Serial Monitor at **115200 baud**. It should print lines like:
   `IMU,GAIT-SHANK,42,1.0000000,0.0000000,0.0000000,0.0000000`
5. Close Serial Monitor so it releases the COM port.
6. In Unity Play mode, go to **Settings → Sensor Connection**, refresh COM ports,
   choose the same port and click **CONNECT USB**. Only valid GAIT-SHANK packets
   produce a receiving-data status. An open port alone is not a sensor reading.
7. Go to **Sensor Calibration**. With the board secured to the shin, capture the
   resting position, then the chosen raised position. Hold still for each capture
   (1.25 seconds). Click **USE SENSOR**. Space remains the tap/hold control.
8. Start gameplay. The marker and seated model use the calibrated sensor height.
   Use **USE MOUSE** to return to the prototype controls, or disconnect the port.

The Unity Windows Player API compatibility profile is .NET Framework so the
built-in `System.IO.Ports` API is available. No third-party serial DLL is added.
The connection component is authored on the existing shared SceneTransition
prefab; no runtime UI GameObjects are generated.

## Data and calibration

The USB protocol is a newline-terminated CSV line:
`IMU,GAIT-SHANK,uint16_sequence,float_w,float_x,float_y,float_z` at 50 Hz.
The parser rejects other segment names, malformed values, non-finite components
and implausible quaternion lengths. Startup text is ignored. Sequence rollover
is accepted and immediate duplicate sequence numbers are ignored.

Unity derives gravity direction in sensor coordinates from the quaternion. It
learns the signed movement axis from resting and raised positions and maps that
tilt interval to 0–100% height, with a short smoothing filter. This avoids using
unobservable absolute yaw as a height signal. The 10–150 degree accepted reference
span is a prototype input-validation range, not a prescribed exercise range.
Repositioning the board requires new calibration. Calibration lasts for this
connection only and must be repeated after reconnecting.

One shin IMU measures a segment's tilt relative to the captured reference. It does
not measure an anatomical knee angle without a thigh reference. Results therefore
continue to show ROM as not measured. Sensor samples do not alter saved settings
or create session records; completing gameplay saves a session through the existing
session-history system.

If data becomes older than 0.5 seconds, sensor-controlled gameplay pauses and
does not silently use the mouse. Reconnect and recalibrate if necessary before
resuming. In a normal connection, restore data and resume manually. Unplugging USB
can change the port number; refresh the COM list after reconnecting.

## Troubleshooting

- **Access denied / cannot open:** close Arduino Serial Monitor, Serial Plotter
  and other applications that have the COM port open.
- **Waiting for data:** verify the USB sketch is uploaded, the board selected is
  correct, and the Serial Monitor shows the exact CSV format above. The original
  BLE sketch will not stream movement through USB.
- **No ports:** check the data-capable USB cable and the port shown by Arduino IDE.
- **Capture failed:** keep the sensor still and wait for steady live readings.
- **Reversed movement:** recapture rest first and raised second. No hard-coded
  sensor axis or left/right mounting orientation is assumed.

## References

- [Arduino Serial.println](https://docs.arduino.cc/language-reference/en/functions/communication/serial/println/)
- [Unity .NET API compatibility](https://docs.unity.com/en-us/engine/6000.7/manual/programming-environment/overview-of-dot-net-in-unity/dotnet-profile-support)

Validation: firmware compile for `Seeeduino:mbed:xiaonRF52840Sense` (installed
2.9.3 core); parser/calibration checks in `Tools/VerifyUsbSensor.cs`; Play-mode
screen and simulated-sample integration checks in `Tools/VerifyUsbSensorLive.cs`
and `Tools/VerifyUsbGameplayLive.cs`. Simulated tests do not verify the USB cable,
mounted-sensor orientation or real device readings.
