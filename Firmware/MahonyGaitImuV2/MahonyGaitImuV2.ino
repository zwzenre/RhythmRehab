/*
  Mahony Gait IMU v2 - Seeed XIAO nRF52840 Sense

  Drift-oriented changes from sensor_code.txt:
  - measured integration dt instead of a fixed 0.01 s;
  - normalized accelerometer feedback;
  - slow gyro-bias tracking only while stationary;
  - 50 Hz BLE output to reduce two-sensor notification pressure;
  - unchanged 18-byte <uint16 sequence, float w/x/y/z> protocol.

  This is still a 6-axis orientation estimate. Absolute yaw remains
  unobservable without a magnetometer or another heading reference.
*/

#include <ArduinoBLE.h>
#include "LSM6DS3.h"
#include "Wire.h"
#include <math.h>

#define SEG_THIGH 0
#define SEG_SHANK 1
#define SEG_FOOT  2
#define SEG_WRIST 3

// Select the sensor position before flashing this board.
// The Patient Portal expects the wrist device to advertise as GAIT-WRIST.
#define SEGMENT SEG_FOOT

#if SEGMENT == SEG_THIGH
  #define SEG_NAME "GAIT-THIGH"
#elif SEGMENT == SEG_SHANK
  #define SEG_NAME "GAIT-SHANK"
#elif SEGMENT == SEG_FOOT
  #define SEG_NAME "GAIT-FOOT"
#elif SEGMENT == SEG_WRIST
  #define SEG_NAME "GAIT-WRIST"
#else
  #error "Unsupported SEGMENT selection"
#endif

BLEService imuService("19B30000-E8F2-537E-4F6C-D104768A1214");
BLECharacteristic quatChar("19B30001-E8F2-537E-4F6C-D104768A1214", BLENotify, 18, true);
LSM6DS3 imu(I2C_MODE, 0x6A);

const float DEG2RAD = 0.01745329252f;
const uint32_t SAMPLE_US = 20000UL; // 50 Hz
const float KP = 4.0f;
const float KI = 0.0f; // Gravity cannot correct absolute yaw.
const float STATIONARY_GYRO_DPS = 0.6f;
const float STATIONARY_ACCEL_TOLERANCE_G = 0.04f;
const float BIAS_TRACK_ALPHA = 0.0015f;

float q0 = 1.0f, q1 = 0.0f, q2 = 0.0f, q3 = 0.0f;
float integralError[3] = {0.0f, 0.0f, 0.0f};
float gyroBias[3] = {0.0f, 0.0f, 0.0f}; // deg/s
uint16_t sequenceNumber = 0;
uint32_t lastSampleUs = 0;

void calibrateGyro(uint16_t samples = 1000) {
  float sums[3] = {0.0f, 0.0f, 0.0f};
  delay(500);
  for (uint16_t index = 0; index < samples; index++) {
    sums[0] += imu.readFloatGyroX();
    sums[1] += imu.readFloatGyroY();
    sums[2] += imu.readFloatGyroZ();
    delay(2);
  }
  for (uint8_t axis = 0; axis < 3; axis++) gyroBias[axis] = sums[axis] / samples;
}

bool isStationary(float ax, float ay, float az, float gx, float gy, float gz) {
  const float accelMagnitude = sqrtf(ax * ax + ay * ay + az * az);
  const float gyroMagnitude = sqrtf(gx * gx + gy * gy + gz * gz);
  return fabsf(accelMagnitude - 1.0f) <= STATIONARY_ACCEL_TOLERANCE_G
      && gyroMagnitude <= STATIONARY_GYRO_DPS;
}

void trackGyroBias(float rawGx, float rawGy, float rawGz) {
  gyroBias[0] += BIAS_TRACK_ALPHA * (rawGx - gyroBias[0]);
  gyroBias[1] += BIAS_TRACK_ALPHA * (rawGy - gyroBias[1]);
  gyroBias[2] += BIAS_TRACK_ALPHA * (rawGz - gyroBias[2]);
}

void mahonyUpdate(float ax, float ay, float az, float gx, float gy, float gz, float dt) {
  const float accelNorm = sqrtf(ax * ax + ay * ay + az * az);
  if (accelNorm < 0.0001f) return;
  ax /= accelNorm;
  ay /= accelNorm;
  az /= accelNorm;

  const float vx = 2.0f * (q1 * q3 - q0 * q2);
  const float vy = 2.0f * (q0 * q1 + q2 * q3);
  const float vz = q0 * q0 - q1 * q1 - q2 * q2 + q3 * q3;

  const float ex = ay * vz - az * vy;
  const float ey = az * vx - ax * vz;
  const float ez = ax * vy - ay * vx;

  if (KI > 0.0f) {
    integralError[0] += ex * dt;
    integralError[1] += ey * dt;
    integralError[2] += ez * dt;
  }

  gx += KP * ex + KI * integralError[0];
  gy += KP * ey + KI * integralError[1];
  gz += KP * ez + KI * integralError[2];

  const float qd0 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
  const float qd1 = 0.5f * ( q0 * gx + q2 * gz - q3 * gy);
  const float qd2 = 0.5f * ( q0 * gy - q1 * gz + q3 * gx);
  const float qd3 = 0.5f * ( q0 * gz + q1 * gy - q2 * gx);

  q0 += qd0 * dt;
  q1 += qd1 * dt;
  q2 += qd2 * dt;
  q3 += qd3 * dt;

  const float quaternionNorm = sqrtf(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
  if (quaternionNorm > 0.0001f) {
    q0 /= quaternionNorm;
    q1 /= quaternionNorm;
    q2 /= quaternionNorm;
    q3 /= quaternionNorm;
  }
}

void setup() {
  Serial.begin(115200);
  if (imu.begin() != 0) {
    Serial.println("IMU init failed");
    while (true) delay(1000);
  }
  if (!BLE.begin()) {
    Serial.println("BLE init failed");
    while (true) delay(1000);
  }

  Serial.print("Keep ");
  Serial.print(SEG_NAME);
  Serial.println(" completely still during gyro calibration...");
  calibrateGyro();

  BLE.setLocalName(SEG_NAME);
  BLE.setDeviceName(SEG_NAME);
  BLE.setAdvertisedService(imuService);
  imuService.addCharacteristic(quatChar);
  BLE.addService(imuService);
  uint8_t initialPacket[18] = {0};
  quatChar.writeValue(initialPacket, sizeof(initialPacket));
  BLE.advertise();
  Serial.print(SEG_NAME);
  Serial.println(" ready: Mahony v2 quaternion stream at 50 Hz");
}

void loop() {
  BLEDevice central = BLE.central();
  if (!central) return;

  lastSampleUs = micros();
  while (central.connected()) {
    BLE.poll();
    const uint32_t nowUs = micros();
    const uint32_t elapsedUs = nowUs - lastSampleUs;
    if (elapsedUs < SAMPLE_US) continue;
    lastSampleUs = nowUs;
    float dt = elapsedUs * 0.000001f;
    if (dt < 0.005f || dt > 0.100f) dt = 0.020f;

    const float ax = imu.readFloatAccelX();
    const float ay = imu.readFloatAccelY();
    const float az = imu.readFloatAccelZ();
    const float rawGx = imu.readFloatGyroX();
    const float rawGy = imu.readFloatGyroY();
    const float rawGz = imu.readFloatGyroZ();

    float correctedGx = rawGx - gyroBias[0];
    float correctedGy = rawGy - gyroBias[1];
    float correctedGz = rawGz - gyroBias[2];
    if (isStationary(ax, ay, az, correctedGx, correctedGy, correctedGz)) {
      trackGyroBias(rawGx, rawGy, rawGz);
      correctedGx = rawGx - gyroBias[0];
      correctedGy = rawGy - gyroBias[1];
      correctedGz = rawGz - gyroBias[2];
    }

    mahonyUpdate(
      ax, ay, az,
      correctedGx * DEG2RAD,
      correctedGy * DEG2RAD,
      correctedGz * DEG2RAD,
      dt
    );

    const float quaternion[4] = {q0, q1, q2, q3};
    uint8_t packet[18];
    memcpy(packet, &sequenceNumber, 2);
    memcpy(packet + 2, quaternion, 16);
    quatChar.writeValue(packet, sizeof(packet));
    sequenceNumber++;
  }
}