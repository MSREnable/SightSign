# EyeSign

EyeSign, a Microsoft Garage project, showcases the power of eye gaze input, inking with Windows, and robotics.  When paired with the uArm (https://www.ufactory.cc/) the robot will sign whatever has been inked into the application.  

👁+🐙+🤖=🎉 

# Hardware

For this project, we used the following hardware:

- [Surface Pro 4](https://www.microsoft.com/surface/en-us/devices/surface-pro-4)
- [Tobii EyeX Controller](http://www.tobii.com/xperience/products/)
- [uArm Metal](https://www.ufactory.cc/en/uarm_metal/)

Most Windows 10 should work, so long as they meet the requirements for the eye tracker you choose. We have tested on Surface Pro 3 and Surface Pro 4.

There are many robotic arms available on the market. We used the uArm because it is readily available, inexpensive, open source, and based on maker friendly hardware.

# Installation

To replicate our experiment

1. Clone the repo
2. Upload firmware to uArm via the Arduino IDE
3. Mount the arm (we mounded ours to be more like a [SCARA robot](robot/readme.md#scara-mode))
4. Connect the arm and the Tobii sensor
5. Calibrate your Tobii sensor
5. Open the solution in Visual Studio and run

# App usage
The app has 3 main modes

1. Stamp – When pressed, a green dot appears on the first part of the signature.  Use eye gaze (or mouse) to have the dot trace the signature.  At the same time, the application will send the entire signature to the robot
2. Write – Similar to stamp, but requires the user to follow the green dot across the entire signature.  As the user completes each stroke, that stroke data is sent to the robot
3. Edit – Enables new inking into the app.  Ink files can be loaded and saved in this mode.
Settings – Enables the robot to be configured for usage by setting the “z” axis to be level with the signing surface

# Robot firmware configuration

To configure the robot firmware, see [robot/readme.md](robot/readme.md).

# Code Notes

The application is a WPF .NET application written in C#, with C/C++ Arduino code for the uArm. Code for the uArm can be found in [robot](robot), while code for the WPF application can be found in [eyeSign](eyeSign). The code is reasonably straight forward, and can be broken down into a few main parts:

- Code to support inking, including loading and saving
- Code to support talking to the robot arm
- Code to support interaction, mainly click handlers

# Accessibility Notes

This application was designed to be accessible in a variety of ways. For full details, please review the [accessibility.md](docs/Accessibility.md).

# Privacy

Information regarding privacy can be found in the [privacy.txt](docs/privacy.txt).

# License

License information can be found in [LICENSE](LICENSE)

---

The Microsoft Garage turns fresh ideas into real projects. Learn more at [http://microsoft.com/garage](http://microsoft.com/garage).
