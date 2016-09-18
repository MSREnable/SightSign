using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace eyeSign
{
    public class RobotArm : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

        private readonly IArm _arm;

        public RobotArm(double xShift, double yShift, double minDimensionHalf, IArm arm)
        {
            _xShift = xShift;
            _yShift = yShift;
            _minDimensionHalf = minDimensionHalf;
            _arm = arm;
            Connect();
        }

        public void Connect()
        {
            Console.WriteLine(@"CONNECT");
            try
            {
                _arm.Connect();
                Connected = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not connect to robot " + ex.Message);
            }
        }

        public void Disconnect()
        {
            if (!Connected) return;

            // Lift the arm.
            ArmDown(false);

            // Now disconnect the arm.
            _arm.Disconnect();
            Connected = false;
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

        public void ArmDown(bool down)
        {
            if (!Connected) return;

            Debug.WriteLine("Arm is " + (down ? "down" : "up"));

            // Set the ArmIsDown property regardless of whether the app is currently controlling
            // the robot, in order for the dot visual bound to the property to be updated.
            ArmIsDown = down;

            Move(LastPoint);
        }

        private bool _scaraMode = true;
        public void MoveRT(double r, double t)
        {
            if (!Connected) return;

            var x = r * Math.Sin(t);
            var y = r * Math.Cos(t);
            var z = (ArmIsDown ? 0.0 : 0.4) - ZShift;
            _arm.Move(x, y, z, _scaraMode);
        }

        private const double ScalingFactorX = 1.2;
        private const double ScalingFactorY = 1.0;

        public void Move(Point pt)
        {
            if (!Connected) return;

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
            if (!Connected) return;

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

        public void Home()
        {
            if (!Connected) return;

            ArmDown(false);
            Move(new Point(0, 0));
        }
    }
}