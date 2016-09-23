# SightSign Robot

The arm being used is the [uArm Metal from uFactory](https://www.ufactory.cc/en/uarm_metal/). We've replaced the firmware with our own.

# Installation

* Load [`firmware.ino`](robot/firmware/firmware.ino) into the [Arduino IDE](https://www.arduino.cc/en/Main/Software)
* Manually drop the [Brief and Reflecta](https://github.com/AshleyF/brief/tree/gh-pages/embedded/Firmware/libraries) libraries into the `Documents/Arduino/libraries` directory
* Compile and upload the sketch
* Note: There is no dependency on [UArmForArduino](https://github.com/uArm-Developer/UArmForArduino)

# uArm Firmware

* Joint control with self-collision constraints
* Cartesian control, including smooth trajectory execution
* Uses [Reflecta](https://github.com/JayBeavers/Reflecta) protocol
* Has [Brief](https://github.com/AshleyF/brief/tree/gh-pages/embedded) bindings

Brief enables quick experimentation within the [Brief Interactive](https://github.com/AshleyF/brief/blob/gh-pages/embedded/Interactive/Program.fs). This, with [Reflecta](https://github.com/JayBeavers/Reflecta) framing, becomes the protocol to control the arm over USB. Custom byte codes are added and scripted against, using the Brief compiler on the PC-side.

## Joint Control

The following constants define joint limits. That is, what is the extent (in radians) of the servos range without the arm self-colliding.

* `TMIN`/`TMAX` - Theta (base) joint limits
* `AMIN`/`AMAX` - Joint A (left servo, controlling first linkage) limits
* `BMIN`/`BMAX` - Joint B (right servo, controlling second linkage) limits

Additionally, a combined limit is set. Since the A/B (left/right) servos are mounted opposite to each other, their combined rotation (simple sum) amounts to how far extended or collapsed the arm position is. It is possible to over-extend or cause collision between the first/second linkage without reaching individual joint limits.

* `CMIN`/`CMAX` - Combined (simple sum of A + B) limits

The `setJoint(...)` API does not handle self-collision at all. However, the `setJoints(...)` API constrains values to stay within joint limits; first within individual limits and then within the combined A/B limits.

The `getJoint(...)` and `getJoints(...)` APIs return the current servo values. If servos are unattached then the arm is free to be manually moved and these APIs report the current actual position.

### Smooth Control

The `trajectoryJoints(...)` API will smoothly transition from the current set of joint positions to the given set, with `wait` controlling speed. Acceleration is not yet included.

### Backlash

Whenever joints change direction, there is a dead band before actual movement begins. To compensate for this, there are `backlash` values applied upon direction change. These are initialized to `0`, but may be set through Brief (instruction `111` bound to the word `backlash!`; taking `theta`, `a`, `b` values from the stack in milliradians).

Backlash correction applies even when using Cartesian control (below).

## Cartesian Control

Basic trigonometry is used to map joint space (radians) to/from Cartesian (x, y, z in centimeters) space. Some constants give the measurements (in cm) of the uArm:

* `BASE_Z` - measured from the work surface to the first joint (base end of first linkage - ~11cm up)
* `BASE_R` - measured from the pivot of the base to the pivot of the first joint (~2cm forward)
* `LINK0` - length of first linkage, from first to second joint (~14.8cm)
* `LINK1` - length of second linkage, from second to third joint (~16cm)

The actual end effector is not taken into account. Instead, the coordinates specify the point of the final joint.

Using the helpers (below), `xyzToJoints()` will do the Cartesian to joint space mapping, while `jointsToXYZ(...)` will do the inverse. The `getXYZ(...)` and `setXYZ(...)` APIs then do Cartesian control.

### Smooth Control

Like with joint control, there is a `trajectoryXYZ(...)` API which will smoothly transition from the current position to the given. Unlike joint trajectories, it also controls the increments of each joint so that all arrive at the target at the same time and the trajectory is _straight_ through Cartesian space.

### Helpers

Helper functions first convert joints to a radius (extension) and z, with theta (base rotation) being known. This is then converted from polar (radius/theta) to x/y and combined with z for full 3D space.

The `jointsToRadius(...)` function converts the A/B joints to a radius and Z. The `radiusZToJoints(...)` function does the inverse.

The `radiusThetaToXY(...)` function merely converts from polar to 2D coordinates, while `xyToRadiusTheta(...)` does the inverse.

## Hybrid Control

For some applications, it may be useful to control position directly in polar coordinates along with Z. This can be done with the `trajectoryRTZ(...)` API (which internally just uses `radiusThetaToXY(...)` and `trajectoryXYZ(...)`).

## SCARA Mode

Speaking of hybrid control, for the SightSign project in particular we mounted the arm _sideways_ and used theta to raise/lower the pen and then treated radius/Z as X/Y. The idea was to physically position the arm such that at a particular base rotation (theta), the A/B joints would cause the arm to move in a plane parallel to the writing surface; this way avoiding any base movement while writing. We called this "[SCARA](https://en.wikipedia.org/wiki/SCARA) Mode". 

You can see [here in UArm.cs](../SightSign/SightSign/UArm.cs#L96-L110) that in `scara` mode it uses RTZ control, treating `x` as radius, `y` as negative Z, and `z` as theta (note that `z` is an _angle_ rather than a coordinate in this case). In non-SCARA mode it uses plain XYZ control.

If you will be using the uArm without modification, then set [the `_scaraMode` flag here](../SightSign/SightSign/RobotArm.cs#L128) to `false`. The issue you may find is that the granularity of base rotation movement causes "jagged" writing. 

### 3D Printed Files 

The [uArm base](parts/uarm%20base.STL) replaces the base that comes with the arm and makes it less slippy.
A pair of [uArm pen holders](parts/uarm%20pen%20holder.STL) replaces the gripper to hold a marker in sideways (SCARA) configuration.

## Brief Bindings

There are zero-operand versions of the above APIs that take their arguments from the Brief stack. These are bound to Brief instructions. To use them in the interactive (which is *very* useful for incremental development without flashing), use something like the following bindings:

    103 'attach    instruction
    104 'detach    instruction
    105 'joint?    instruction
    106 'joint!    instruction
    107 'joints!   instruction
    108 'xyz?      instruction
    109 'xyz!      instruction
    110 'xyz!!     instruction
    110 'backlash! instruction
    111 'joints!!  instruction
    113 'rtz!!     instruction

You can then connect (e.g. `'com4 conn` or `'/dev/ttyUSB0 conn`) and inspect or set joints (e.g. `123 456 789 joints!!`), set backlash parameters (e.g. `123 0 backlash!`), Cartesian positions (e.g. `123 456 789 xyz!`), etc.

For example, this was very useful in determining the joint limit values we later embedded and for quickly experimenting with backlash correction.

This is how we ultimately talk to the arm through compiled Brief sent as Reflecta frames. The `UArm` class [makes these instruction bindings](../SightSign/SightSign/UArm.cs#L68-L71) and compiles fragments to execute movements, attach/detach, ...
