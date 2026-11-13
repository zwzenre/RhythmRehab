  /*
  RhythmRehab USB shin IMU — Seeed XIAO nRF52840 Sense (mbed-enabled board).
  Adapted from the supplied Mahony Gait IMU v2 sketch.
  Keep the board still at power-on for the ~2.5 second gyro calibration.

  USB output, 115200 baud, 50 Hz:
    IMU,GAIT-SHANK,sequence,w,x,y,z\n
  Startup messages begin with # and are ignored by Unity.
  This USB-only sketch does not require a Bluetooth central to start sampling.
  Keep your original BLE sketch separately if it is needed for another application.
*/
#include "LSM6DS3.h"
#include "Wire.h"
#include <math.h>

LSM6DS3 imu(I2C_MODE, 0x6A);
const float DEG2RAD = 0.01745329252f;
const uint32_t SAMPLE_US = 20000UL;
const float KP = 4.0f;
const float STATIONARY_GYRO_DPS = 0.6f;
const float STATIONARY_ACCEL_TOLERANCE_G = 0.04f;
const float BIAS_TRACK_ALPHA = 0.0015f;
float q0 = 1, q1 = 0, q2 = 0, q3 = 0;
float gyroBias[3] = {0, 0, 0};
uint16_t sequenceNumber = 0;
uint32_t lastSampleUs = 0;

void calibrateGyro(uint16_t samples = 1000) {
  float sums[3] = {0, 0, 0};
  delay(500);
  for (uint16_t i = 0; i < samples; i++) {
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
  return fabsf(accelMagnitude - 1) <= STATIONARY_ACCEL_TOLERANCE_G && gyroMagnitude <= STATIONARY_GYRO_DPS;
}

void mahonyUpdate(float ax, float ay, float az, float gx, float gy, float gz, float dt) {
  const float accelNorm = sqrtf(ax * ax + ay * ay + az * az);
  if (accelNorm < 0.0001f) return;
  ax /= accelNorm; ay /= accelNorm; az /= accelNorm;
  const float vx = 2 * (q1 * q3 - q0 * q2);
  const float vy = 2 * (q0 * q1 + q2 * q3);
  const float vz = q0 * q0 - q1 * q1 - q2 * q2 + q3 * q3;
  gx += KP * (ay * vz - az * vy);
  gy += KP * (az * vx - ax * vz);
  gz += KP * (ax * vy - ay * vx);
  const float qd0 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
  const float qd1 = 0.5f * ( q0 * gx + q2 * gz - q3 * gy);
  const float qd2 = 0.5f * ( q0 * gy - q1 * gz + q3 * gx);
  const float qd3 = 0.5f * ( q0 * gz + q1 * gy - q2 * gx);
  q0 += qd0 * dt; q1 += qd1 * dt; q2 += qd2 * dt; q3 += qd3 * dt;
  const float norm = sqrtf(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
  if (norm > 0.0001f) { q0 /= norm; q1 /= norm; q2 /= norm; q3 /= norm; }
}

void setup() {
  Serial.begin(115200);
  // Do not wait for Serial: the IMU should run even when Unity has not opened USB.
  if (imu.begin() != 0) {
    while (true) {
      if (Serial) Serial.println("# IMU init failed");
      delay(1000);
    }
  }
  if (Serial) Serial.println("# Keep GAIT-SHANK completely still during gyro calibration...");
  calibrateGyro();
  lastSampleUs = micros();
}

void loop() {
  const uint32_t nowUs = micros();
  const uint32_t elapsedUs = nowUs - lastSampleUs;
  if (elapsedUs < SAMPLE_US) return;
  lastSampleUs = nowUs;
  float dt = elapsedUs * 0.000001f;
  if (dt < 0.005f || dt > 0.100f) dt = 0.020f;

  const float ax = imu.readFloatAccelX(), ay = imu.readFloatAccelY(), az = imu.readFloatAccelZ();
  const float rawGx = imu.readFloatGyroX(), rawGy = imu.readFloatGyroY(), rawGz = imu.readFloatGyroZ();
  float gx = rawGx - gyroBias[0], gy = rawGy - gyroBias[1], gz = rawGz - gyroBias[2];
  if (isStationary(ax, ay, az, gx, gy, gz)) {
    gyroBias[0] += BIAS_TRACK_ALPHA * (rawGx - gyroBias[0]);
    gyroBias[1] += BIAS_TRACK_ALPHA * (rawGy - gyroBias[1]);
    gyroBias[2] += BIAS_TRACK_ALPHA * (rawGz - gyroBias[2]);
    gx = rawGx - gyroBias[0]; gy = rawGy - gyroBias[1]; gz = rawGz - gyroBias[2];
  }
  mahonyUpdate(ax, ay, az, gx * DEG2RAD, gy * DEG2RAD, gz * DEG2RAD, dt);

  // This Seeed mbed core inherits Print::availableForWrite(), which returns 0.
  // Check the USB connection itself instead of gating every frame on that API.
  if (Serial) {
    Serial.print("IMU,GAIT-SHANK,"); Serial.print(sequenceNumber);
    Serial.print(','); Serial.print(q0, 7);
    Serial.print(','); Serial.print(q1, 7);
    Serial.print(','); Serial.print(q2, 7);
    Serial.print(','); Serial.println(q3, 7);
  }
  sequenceNumber++;
}
