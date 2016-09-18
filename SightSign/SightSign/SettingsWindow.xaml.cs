using System.Windows;

namespace SightSign
{
    // A window showing the configurable settings in the app. 
    public partial class SettingsWindow : Window
    {
        private MainWindow _mainWindow;
        private Settings _settings;
        private RobotArm _robotArm;

        private bool _windowIntialized;

        public SettingsWindow(
            MainWindow mainWindow,
            Settings settings, 
            RobotArm robotArm)
        {
            _mainWindow = mainWindow;
            _settings = settings;
            _robotArm = robotArm;

            InitializeComponent();

            RobotControlCheckBox.IsChecked = _settings.RobotControl;

            // Explicitly disable the owning window here, to prevent any interaction 
            // through eyegaze input while the Settings window is visible.
            Loaded += (s, e) => Owner.IsEnabled = false;
            Unloaded += (s, e) =>
            {
                Owner.IsEnabled = true;
                mainWindow.Focus();
            };

            _windowIntialized = true;
        }

        private void RobotControlCheckBox_CheckedStateChanged(object sender, RoutedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            _settings.RobotControl = RobotControlCheckBox.IsChecked.Value;

            if (_settings.RobotControl)
            {
                _robotArm.Connect();
            }
            else
            {
                _robotArm.Disconnect(); // must disconnect *before* disabling
            }

            Settings1.Default.RobotControl = _settings.RobotControl;
            Settings1.Default.Save();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ZUpButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.RobotArm.ArmDown(true);
            _mainWindow.RobotArm.ZShift -= 0.02;
        }

        private void ZDownButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.RobotArm.ArmDown(true);
            _mainWindow.RobotArm.ZShift += 0.02;
        }

        private void ShowCornersButton_Click(object sender, RoutedEventArgs e)
        {
            // Find the bounding rectangle of all the strokes in the ink.
            var countStrokes = _mainWindow.inkCanvas.Strokes.Count;

            var bounds = new Rect();

            for (var i = 0; i < countStrokes; ++i)
            {
                if (i == 0)
                {
                    bounds = _mainWindow.inkCanvas.Strokes.GetBounds();
                }
                else
                {
                    bounds.Union(_mainWindow.inkCanvas.Strokes.GetBounds());
                }
            }

            // Now draw dots at the four corners of the bounding rect.
            if (!bounds.IsEmpty)
            {
                _mainWindow.RobotArm.ArmDown(false);
                _robotArm.Move(new Point(bounds.X, bounds.Y));
                _mainWindow.RobotArm.ArmDown(true);

                _mainWindow.RobotArm.ArmDown(false);
                _robotArm.Move(new Point(bounds.X + bounds.Width, bounds.Y));
                _mainWindow.RobotArm.ArmDown(true);

                _mainWindow.RobotArm.ArmDown(false);
                _robotArm.Move(new Point(bounds.X + bounds.Width, bounds.Y + bounds.Height));
                _mainWindow.RobotArm.ArmDown(true);

                _mainWindow.RobotArm.ArmDown(false);
                _robotArm.Move(new Point(bounds.X, bounds.Y + bounds.Height));
                _mainWindow.RobotArm.ArmDown(true);

                // Leave the robot arm up.
                _mainWindow.RobotArm.ArmDown(false);
            }
        }
    }
}
