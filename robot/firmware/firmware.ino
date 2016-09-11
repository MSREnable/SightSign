#include <Brief.h>
#include <Servo.h>

#define VER 1

// potentially arm-specific measurements and calibration parameters

// min/max before self-collision at each servo
//#define PI 3.14159265358979323846
#define TMIN 0
#define TMAX PI
#define AMIN -0.3
#define AMAX  2.4
#define BMIN  0.1
#define BMAX  2.4

// combined (sum) min/max of A and B before arm self-collision or over extension
#define CMIN 0.5
#define CMAX 2.8

#define RAD_PER_DEG (180.0 / PI)

// servo control

Servo servoT, servoA, servoB;

void setServoStatus(bool state, int num) {
  switch (num) {
    case 0: if (state) servoT.attach(11); else servoT.detach();
    case 1: if (state) servoA.attach(13); else servoA.detach();
    case 2: if (state) servoB.attach(12); else servoB.detach();
  }
}

double readServoAngle(int num) {
  switch (num) {
    case 0: return servoT.read();
    case 1: return servoA.read();
    case 2: return servoB.read();
  }
}

void writeServoAngle(int num, double deg) {
  switch (num) {
    case 0: servoT.write(deg); break;
    case 1: servoA.write(deg); break;
    case 2: servoB.write(deg); break;
  }
}

// joint control

#define SERVO_THETA_NUM 0
#define SERVO_LEFT_NUM 1
#define SERVO_RIGHT_NUM 2

double toRadians(double x) { return x / RAD_PER_DEG; }
double toDegrees(double x) { return x * RAD_PER_DEG; }

void attachAll(bool state) {
  setServoStatus(state, SERVO_THETA_NUM);
  setServoStatus(state, SERVO_LEFT_NUM);
  setServoStatus(state, SERVO_RIGHT_NUM);
}

double getJoint(int j) { return toRadians(readServoAngle(j)); }

void setJoint(double v, int j) { // WARNING: allows self-collision!
  writeServoAngle(j, toDegrees(v));
}

void getJoints(double& t, double& a, double& b) {
  t = getJoint(SERVO_THETA_NUM);
  a = getJoint(SERVO_LEFT_NUM);
  b = getJoint(SERVO_RIGHT_NUM);
}

void setJoints(double t, double a, double b) { // radians
  // avoid self-collision (joint constaints and combined A/B collapsed/extended position)
  t = max(TMIN, min(TMAX, t));
  a = max(AMIN, min(AMAX, a));
  b = max(max(BMIN, CMIN - a), min(min(BMAX, CMAX - a), b));
  setJoint(t, SERVO_THETA_NUM);
  setJoint(a, SERVO_LEFT_NUM);
  setJoint(b, SERVO_RIGHT_NUM);
}

double backlash[] { 0, 0, 0 }; // set externally (see brief_configBacklash)
double dir[] { 0, 0, 0 };
double current[] { 0, 0, 0 };

double adjustForBacklash(double v, int j) {
  double delta = v - current[j];
  current[j] = v;
  if (delta < 0) dir[j] = -1.0;
  if (delta > 0) dir[j] = 1.0;
  return v + dir[j] * backlash[j];
}

double cur_t = 0, cur_a = 0, cur_b = 0;

void trajectoryJoints(double t, double a, double b, int w) { // TODO: factor into generic trajectory (through joint-space or cartesian-space, with `set` function passed in)
  t = adjustForBacklash(t, 0);
  a = adjustForBacklash(a, 1);
  b = adjustForBacklash(b, 2);
  //getJoints(cur_t, cur_a, cur_b); // done at attach-time in Brief
  double td = t - cur_t;
  double ad = a - cur_a;
  double bd = b - cur_b;
  double d = sqrt(td * td + ad * ad + bd * bd);
  double steps = d * 10; // centiradians
  td /= steps;
  ad /= steps;
  bd /= steps;
  for (int i = 0; i < steps - 1; i++) {
    cur_t += td;
    cur_a += ad;
    cur_b += bd;
    setJoints(cur_t, cur_a, cur_b);
    delayMicroseconds(w);
  }
  cur_t = t;
  cur_a = a;
  cur_b = b;
  setJoints(t, a, b);
}

// cartesian control

// measurments (mm) of arm links
#define BASE_Z 11.245
#define BASE_R  2.117
#define LINK0  14.825
#define LINK1  16.020

void jointsToRadiusZ(double a, double b, double& r, double& z) { // A/B joint angles to radius/z
  r = BASE_R + LINK0 * cos(a) + LINK1 * cos(b);
  z = BASE_Z + LINK0 * sin(abs(a)) - LINK1 * sin(abs(b));
}

void radiusZToJoints(double r, double z, double& a, double& b) {
  r -= BASE_R;
  z -= BASE_Z;
  double a2 = LINK0 * LINK0;
  double b2 = LINK1 * LINK1;
  double h2 = r * r + z * z;
  double h2_2 = 2.0 * h2;
  double h2_4 = 4.0 * h2;
  double aa = a2 - b2 + h2;
  double bb = b2 - a2 + h2;
  a = acos((r * aa - z * sqrt(a2 * h2_4 - aa * aa)) / (h2_2 * LINK0));
  b = acos((r * bb + z * sqrt(b2 * h2_4 - bb * bb)) / (h2_2 * LINK1));
}

void radiusThetaToXY(double r, double t, double& x, double& y) {
  x = r * sin(t); // right-hand coords (x = y)
  y = -(r * cos(t)); // right-hand coords (y = -x)
}

void xyToRadiusTheta(double x, double y, double& r, double& t) {
  r = sqrt(x * x + y * y);
  t = atan2(x, -y); // right-hand coords (x = -y, y = x)
}

void jointsToXYZ(double t, double a, double b, double& x, double& y, double& z) {
  double r = 0;
  jointsToRadiusZ(a, b, r, z);
  radiusThetaToXY(r, t, x, y);
}

void xyzToJoints(double x, double y, double z, double& t, double& a, double& b) {
  double r = 0;
  xyToRadiusTheta(x, y, r, t);
  radiusZToJoints(r, z, a, b);
}

double getXYZ(double& x, double& y, double& z) {
  double t = 0, a = 0, b = 0;
  getJoints(t, a, b);
  jointsToXYZ(t, a, b, x, y, z);
}

void setXYZ(double x, double y, double z) {
  double t = 0, a = 0, b = 0;
  xyzToJoints(x, y, z, t, a, b);
  setJoints(t, a, b);
}

double cur_x = 0, cur_y = 0, cur_z = 0;

void trajectoryXYZ(double x, double y, double z, int w) {
  //getXYZ(cur_x, cur_y, cur_z); // done at attach-time in Brief
  double xd = x - cur_x;
  double yd = y - cur_y;
  double zd = z - cur_z;
  double d = sqrt(xd * xd + yd * yd + zd * zd);
  double steps = d * 20; // 1/2 millimeters
  xd /= steps;
  yd /= steps;
  zd /= steps;
  for (int i = 0; i < steps - 1; i++) {
    cur_x += xd;
    cur_y += yd;
    cur_z += zd;
    double t = 0, a = 0, b = 0;
    xyzToJoints(cur_x, cur_y, cur_z, t, a, b);
    trajectoryJoints(t, a, b, w);
    delayMicroseconds(w);
  }
  cur_x = x;
  cur_y = y;
  cur_z = z;
  setXYZ(x, y, z);
}

void trajectoryRTZ(double r, double t, double z, int w) {
  double x, y;
  radiusThetaToXY(r, t, x, y);
  trajectoryXYZ(x, y, z, w);
}

// zero-op functions for Brief binding

double fixedToDouble(int16_t x) { return (double)x / 1000.0; }
int16_t doubleToFixed(double x) { return (int16_t)(x * 1000.0); }

void brief_attach() {
  attachAll(true);
  getJoints(cur_t, cur_a, cur_b);
  getXYZ(cur_x, cur_y, cur_z);
}

void brief_detach() { attachAll(false); }

int16_t joint(int16_t j) { return doubleToFixed(getJoint(j)); }
void brief_getJoint() { brief::push(joint(brief::pop())); }

void brief_setJoint() { setJoint(fixedToDouble(brief::pop()), brief::pop()); }

void brief_setJoints() {
  double b = fixedToDouble(brief::pop());
  double a = fixedToDouble(brief::pop());
  double t = fixedToDouble(brief::pop());
  setJoints(t, a, b);
}

void brief_configBacklash() {
  backlash[brief::pop()] = fixedToDouble(brief::pop());
}

void brief_getXYZ() {
  double x = 0, y = 0, z = 0;
  getXYZ(x, y, z);
  brief::push(doubleToFixed(x));
  brief::push(doubleToFixed(y));
  brief::push(doubleToFixed(z));
}

void brief_setXYZ() {
  double z = fixedToDouble(brief::pop());
  double y = fixedToDouble(brief::pop());
  double x = fixedToDouble(brief::pop());
  setXYZ(x, y, z);
}

void brief_trajectoryXYZ() {
  int w = brief::pop();
  double z = fixedToDouble(brief::pop());
  double y = fixedToDouble(brief::pop());
  double x = fixedToDouble(brief::pop());
  trajectoryXYZ(x, y, z, w);
}

void brief_trajectoryRTZ() {
  int w = brief::pop();
  double z = fixedToDouble(brief::pop());
  double t = fixedToDouble(brief::pop());
  double r = fixedToDouble(brief::pop());
  trajectoryRTZ(r, t, z, w);
}

void brief_trajectoryJoints() {
  int w = brief::pop();
  double b = fixedToDouble(brief::pop());
  double a = fixedToDouble(brief::pop());
  double t = fixedToDouble(brief::pop());
  trajectoryJoints(t, a, b, w);
}

void brief_version() {
  brief::push(VER);
}

// Brief bindings

void setup() {
  brief::setup();
  reflectaFrames::setup(19200);
  brief::bind(103, brief_attach);
  brief::bind(104, brief_detach);
  brief::bind(105, brief_getJoint);
  brief::bind(106, brief_setJoint);
  brief::bind(107, brief_setJoints);
  brief::bind(108, brief_getXYZ);
  brief::bind(109, brief_setXYZ);
  brief::bind(110, brief_trajectoryXYZ);
  brief::bind(111, brief_configBacklash);
  brief::bind(112, brief_trajectoryJoints);
  brief::bind(113, brief_trajectoryRTZ);
  brief::bind(114, brief_version);

  attachAll(false);
}

void loop() {
  brief::loop();
  reflectaFrames::loop();
}
