using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Robotics.UArm;

namespace eyeSign
{
    public class RobotArm : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly Grid _canvas;
        private readonly InkCanvas _inkCanvas;
        private readonly double _xShift;
        private readonly double _yShift;
        private double _zShift = Settings1.Default.RobotZShift;
        private readonly double _minDimensionHalf;

        public Point LastPoint { get; set; }

        public double ZShift
        {
            get { return _zShift; }
            set
            {
                _zShift = value;
                Move(LastPoint);
                Settings1.Default.RobotZShift = _zShift;
                Settings1.Default.Save();
                Trace.WriteLine($"ZShift: {_zShift}");
            }
        }

        public UArm Arm = new UArm(Settings1.Default.RobotComPort);

        public RobotArm(double xShift, double yShift, double minDimensionHalf, InkCanvas inkCanvas, Grid canvas)
        {
            _xShift = xShift;
            _yShift = yShift;
            _minDimensionHalf = minDimensionHalf;
            _inkCanvas = inkCanvas;
            _canvas = canvas;

            if (Enabled)
            {
                Connect();
            }
        }

        public void Connect()
        {
            Console.WriteLine(@"CONNECT");
            try
            {
                Arm.Connect();
                Arm.ZeroG(false);

                this.Connected = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not connect to robot " + ex.Message);
            }
        }

        public void Disconnect()
        {
            // Lift the arm.
            ArmDown(false);

            // Now disconnect the arm.
            Arm.ZeroG(true);
            Arm.Disconnect();

            this.Connected = false;
        }

        public void Close()
        {
            Disconnect();
        }

        private bool _connected;
        public bool Connected
        {
            get
            {
                return _connected;
            }

            set
            {
                if (_connected != value)
                {
                    _connected = value;
                    OnPropertyChanged("Connected");
                }
            }
        }

        private bool _armIsDown;
        public bool ArmIsDown
        {
            get
            {
                return _armIsDown;
            }

            set
            {
                if (_armIsDown != value)
                {
                    _armIsDown = value;
                    OnPropertyChanged("ArmIsDown");
                }
            }
        }

        // If the arm's Enabled state is false, then all calls into the RobotArm class are no-ops.
        // TODO: Should this affect the attempt to connect and disconnect to the robot?
        public bool Enabled { get; set; }

        public void SendFullSignature()
        {
            if (!Enabled)
            {
                return;
            }

            // Beware of an inkless canvas.
            if (_inkCanvas.Strokes.Count == 0)
            {
                return;
            }

            // Send each stroke in turn.
            foreach (var stroke in _inkCanvas.Strokes)
            {
                SendStroke(stroke);
            }
        }

        public void SendStroke(Stroke stroke)
        {
            if (!Enabled)
            {
                return;
            }

            // Beware of pointless strokes.
            if (stroke.StylusPoints.Count == 0)
            {
                return;
            }

            // Pen up.
            ArmDown(false);

            // Move to first point on the stroke.
            Move(stroke.StylusPoints[0].ToPoint());

            // Pen down.
            ArmDown(true);

            // Write the entire stroke.
            for (var i = 0; i < stroke.StylusPoints.Count; ++i)
            {
                Move(stroke.StylusPoints[i].ToPoint());
            }

            // Pen up.
            ArmDown(false);
        }

        public void ArmDown(bool down)
        {
            Debug.WriteLine("Arm is " + (down ? "down" : "up"));

            // Set the ArmIsDown property regardless of whether the app is currently controlling
            // the robot, in order for the dot visual bound to the property to be updated.
            ArmIsDown = down;

            if (!Enabled)
            {
                return;
            }

            Move(LastPoint);
        }

        private bool _scaraMode = true;
        public void MoveRT(double r, double t)
        {
            var x = r * Math.Sin(t);
            var y = r * Math.Cos(t);
            var z = (ArmIsDown ? 0.0 : 0.4) - ZShift;
            if (_scaraMode)
            {
                // in scara mode, up is base rotation
                Arm.MoveRTZ(x, z, -y);
            }
            else
            {
                Arm.Move(x, y, z);
            }
        }

        private const double ScalingFactorX = 1.2;
        private const double ScalingFactorY = 1.0;

        public void Move(Point pt)
        {
            if (!Enabled)
            {
                return;
            }

            LastPoint = pt;
            var scale = Settings1.Default.RobotWorkspaceScale;
            var x = ((pt.Y - _yShift) / _minDimensionHalf * scale * ScalingFactorX);
            var y = ((pt.X - _xShift) / _minDimensionHalf * scale * ScalingFactorY);

            // convert to polar to compute backlash
            var r = Math.Sqrt(x * x + y * y);
            var t = Math.Atan2(x, y); // right-hand coords (x = -y, y = x)

            MoveRT(r, t);
        }

        public void CircleTest()
        {
            // this is useful for observing backlash
            const double r = 300;
            const double xOffset = 450;
            const double yOffset = 350;
            ArmDown(false);
            for (var a = 0.0; a < Math.PI * 4; a += 0.1) // twice around
            {
                var x = Math.Cos(a) * r;
                var y = Math.Sin(a) * r;
                if (!_armIsDown)
                {
                    Move(new Point(x + xOffset, y + yOffset));
                    ArmDown(true);
                }
                Move(new Point(x + xOffset, y + yOffset));
            }
            ArmDown(false);
        }

        private const double FactorR = 100.0;
        private const double FactorT = 100.0;

        public void MoveRT(Point p)
        {
            // move in screen X/Y conterted to R/T
            MoveRT(p.X / FactorR, p.Y / FactorT);
        }

        public void Home()
        {
            ArmDown(false);
            Move(new Point(0, 0));
        }
    }
}