# EyeSign Robot

The arm being used is the [uArm Metal from uFactory](https://www.ufactory.cc/en/uarm_metal/). We've replaced the firmware with our own.

# uArm Firmware

Firmware handles:

* Joint control with self-collision constraints
* Cartesian control, including smooth trajectory execution
* Uses [Reflecta](https://github.com/JayBeavers/Reflecta) protocol
* Has [Brief](https://github.com/AshleyF/brief/tree/gh-pages/embedded) bindings

Brief enables quick experimentation within the [Brief Interactive](https://github.com/AshleyF/brief/blob/gh-pages/embedded/Interactive/Program.fs). This, with [Reflecta](https://github.com/JayBeavers/Reflecta) framing, becomes the protocol to control the arm over USB. Custom byte codes are added and scripted against, using the Brief compiler on the PC-side.

To install the firmware:

* Load [`firmware.ino`](https://github.com/MSREnable/EyeSign/blob/master/robot/firmware/firmware.ino) into the [Arduino IDE](https://www.arduino.cc/en/Main/Software)
* Manually drop the [Brief and Reflecta](https://github.com/AshleyF/brief/tree/gh-pages/embedded/Firmware/libraries) libraries into the `Documents/Arduino/libraries` directory
* Compile and upload the sketch
* Note: There is no dependency on [UArmForArduino](https://github.com/uArm-Developer/UArmForArduino)
