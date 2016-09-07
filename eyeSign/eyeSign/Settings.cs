using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace eyeSign
{
    // While the Settings class supports INotifyPropertyChanged, no-one's listening
    // for related property changes yet. 

    public class Settings : INotifyPropertyChanged
    {
        private Color _backgroundColor;
        private Color _buttonBackgroundColor;
        private Color _buttonTextColor;
        private Color _buttonBorderColor;
        private double _buttonBorderWidth;

        private double _inkWidth;
        private double _dotWidth;
        private double _dotDownWidth;
        private Color _inkColor;
        private Color _fadedInkColor;
        private Color _dotColor;
        private Color _dotDownColor;
        private int _animationInterval;
        private int _animationPointsOnFirstStroke;
        private Matrix _inkMatrix = new Matrix();
        private bool _robotControl = false;

        private RobotArm _robotArm;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Settings(RobotArm robotArm)
        {
            Arm = robotArm;
        }

        public double InkWidth
        {
            get
            {
                return _inkWidth;
            }

            set
            {
                // TODO consider changing to tolerance comparison, since this is double
                if (_inkWidth != value)
                {
                    _inkWidth = value;
                    OnPropertyChanged("InkWidth");
                }
            }
        }

        public Color InkColor
        {
            get
            {
                return _inkColor;
            }

            set
            {
                if (_inkColor != value)
                {
                    _inkColor = value;
                    OnPropertyChanged("InkColor");
                }
            }
        }

        public Color FadedInkColor
        {
            get
            {
                return _fadedInkColor;
            }

            set
            {
                if (_fadedInkColor != value)
                {
                    _fadedInkColor = value;
                    OnPropertyChanged("FadedInkColor");
                }
            }
        }

        public int AnimationInterval
        {
            get
            {
                return _animationInterval;
            }

            set
            {
                if (_animationInterval != value)
                {
                    _animationInterval = value;
                    OnPropertyChanged("AnimationInterval");
                }
            }
        }

        public int AnimationPointsOnFirstStroke
        {
            get
            {
                return _animationPointsOnFirstStroke;
            }

            set
            {
                if (_animationPointsOnFirstStroke != value)
                {
                    _animationPointsOnFirstStroke = value;
                    OnPropertyChanged("AnimationPointsOnFirstStroke");
                }
            }
        }

        public Matrix InkMatrix
        {
            get
            {
                return _inkMatrix;
            }

            set
            {
                if (_inkMatrix != value)
                {
                    _inkMatrix = value;
                    OnPropertyChanged("InkMatrix");
                }
            }
        }

        public bool RobotControl
        {
            get
            {
                return _robotControl;
            }

            set
            {
                if (_robotControl != value)
                {
                    _robotControl = value;
                    OnPropertyChanged("RobotControl");
                }
            }
        }

        public Color DotColor
        {
            get
            {
                return _dotColor;
            }

            set
            {
                if (_dotColor != value)
                {
                    _dotColor = value;
                    OnPropertyChanged("DotColor");
                }
            }
        }

        public Color DotDownColor
        {
            get
            {
                return _dotDownColor;
            }

            set
            {
                if (_dotDownColor != value)
                {
                    _dotDownColor = value;
                    OnPropertyChanged("DotDownColor");
                }
            }
        }

        public double DotWidth
        {
            get
            {
                return _dotWidth;
            }

            set
            {
                // TODO consider changing to tolerance comparison, since this is double
                if (_dotWidth != value)
                {
                    _dotWidth = value;
                    OnPropertyChanged("DotWidth");
                }
            }
        }

        public double DotDownWidth
        {
            get
            {
                return _dotDownWidth;
            }

            set
            {
                // TODO consider changing to tolerance comparison, since this is double
                if (_dotDownWidth != value)
                {
                    _dotDownWidth = value;
                    OnPropertyChanged("DotDownWidth");
                }
            }
        }

        // Note that the app does not react at the time a high contrast theme becomes active.
        // There doesn't seem to be a notification for this in WPF.

        public Color BackgroundColor
        {
            get
            {
                return SystemParameters.HighContrast ?
                    SystemColors.WindowColor : _backgroundColor;
            }

            set
            {
                _backgroundColor = value;
            }
        }

        public Color ButtonBackgroundColor
        {
            get
            {
                return SystemParameters.HighContrast ?
                    SystemColors.ControlColor : _buttonBackgroundColor;
            }

            set
            {
                _buttonBackgroundColor = value;
            }
        }

        public Color ButtonTextColor
        {
            get
            {
                return SystemParameters.HighContrast ?
                    SystemColors.ControlTextColor : _buttonTextColor;
            }

            set
            {
                _buttonTextColor = value;
            }
        }

        public Color ButtonBorderColor
        {
            get
            {
                return SystemParameters.HighContrast ?
                    SystemColors.ControlTextColor : _buttonBorderColor;
            }

            set
            {
                _buttonBorderColor = value;
            }
        }

        public double ButtonBorderWidth
        {
            get
            {
                return _buttonBorderWidth;
            }

            set
            {
                _buttonBorderWidth = value;
            }
        }

        public RobotArm Arm
        {
            get
            {
                return _robotArm;
            }

            set
            {
                _robotArm = value;
            }
        }

        // Load all persisted settings.
        public void LoadSettings()
        {
            RobotControl = Settings1.Default.RobotControl;

            var backgroundColorSetting = Settings1.Default.BackgroundColor;
            BackgroundColor = Color.FromRgb(backgroundColorSetting.R, backgroundColorSetting.G, backgroundColorSetting.B);

            var buttonBackgroundColorSetting = Settings1.Default.ButtonBackgroundColor;
            ButtonBackgroundColor = Color.FromRgb(buttonBackgroundColorSetting.R, buttonBackgroundColorSetting.G, buttonBackgroundColorSetting.B);

            var buttonTextColorSetting = Settings1.Default.ButtonTextColor;
            ButtonTextColor = Color.FromRgb(buttonTextColorSetting.R, buttonTextColorSetting.G, buttonTextColorSetting.B);

            var buttonBorderColorSetting = Settings1.Default.ButtonBorderColor;
            ButtonBorderColor = Color.FromRgb(buttonBorderColorSetting.R, buttonBorderColorSetting.G, buttonBorderColorSetting.B);

            ButtonBorderWidth = Settings1.Default.ButtonBorderWidth;

            var inkColorSetting = Settings1.Default.InkColor;
            InkColor = Color.FromRgb(inkColorSetting.R, inkColorSetting.G, inkColorSetting.B);
            InkWidth = Settings1.Default.InkWidth;

            AnimationInterval = Settings1.Default.AnimationInterval;
            AnimationPointsOnFirstStroke = Settings1.Default.AnimationPointsOnFirstStroke;

            var fadedInkColorSetting = Settings1.Default.FadedInkColor;
            FadedInkColor = Color.FromArgb(
                fadedInkColorSetting.A, fadedInkColorSetting.R, fadedInkColorSetting.G, fadedInkColorSetting.B);

            var dotColorSetting = Settings1.Default.DotColor;
            DotColor = Color.FromArgb(
                dotColorSetting.A, dotColorSetting.R, dotColorSetting.G, dotColorSetting.B);

            var dotDownColorSetting = Settings1.Default.DotDownColor;
            DotDownColor = Color.FromArgb(
                dotDownColorSetting.A, dotDownColorSetting.R, dotDownColorSetting.G, dotDownColorSetting.B);

            DotWidth = Settings1.Default.DotWidth;
            DotDownWidth = Settings1.Default.DotDownWidth;

            var inkMatrix = Settings1.Default.InkMatrix;
            if (!string.IsNullOrEmpty(inkMatrix))
            {
                InkMatrix = Matrix.Parse(inkMatrix);
            }
        }
    }
}
