using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using Microsoft.Win32;

namespace eyeSign
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private MainWindow _mainWindow;
        private Settings _settings;
        private RobotArm _robotArm;
        private InkCanvas _inkCanvas;
        private InkCanvas _inkCanvasAnimations;

        private bool _windowIntialized;

        public SettingsWindow(
            MainWindow mainWindow,
            Settings settings, 
            RobotArm robotArm, 
            InkCanvas inkCanvas,
            InkCanvas inkCanvasAnimations)
        {
            _mainWindow = mainWindow;
            _settings = settings;
            _robotArm = robotArm;
            _inkCanvas = inkCanvas;
            _inkCanvasAnimations = inkCanvasAnimations;

            InitializeComponent();

            LoadSettings();

            Loaded += (s, e) => Owner.IsEnabled = false;
            Unloaded += (s, e) =>
            {
                Owner.IsEnabled = true;
                mainWindow.Focus();
            };

            _windowIntialized = true;
        }

        // Load all persisted settings.
        private void LoadSettings()
        {
            RobotControlCheckBox.IsChecked = _settings.RobotControl;
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
                _robotArm.Enabled = true;
                _robotArm.Connect();
            }
            else
            {
                _robotArm.Disconnect(); // must disconnect *before* disabling
                _robotArm.Enabled = false;
            }

            Settings1.Default.RobotControl = _settings.RobotControl;
            Settings1.Default.Save();
        }

        private void ImportXMLButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();

            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";

            var result = dlg.ShowDialog();
            if (result == true)
            {
                try
                {
                    _mainWindow.LoadInkFromXmlFile(dlg.FileName);

                    // We successfully loaded up the ink from the XML data, so save the file location.
                    Settings1.Default.LoadedInkLocation = dlg.FileName;
                    Settings1.Default.Save();

                    _mainWindow.ApplySettingsToInk();
                }
                catch (Exception)
                {
                }
            }
        }

        private void ClearInkButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ResetWriting();

            _inkCanvas.Strokes.Clear();
            _inkCanvasAnimations.Strokes.Clear();
        }

        private void LoadInkButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();

            dlg.DefaultExt = ".isf";
            dlg.Filter = "ISF files (*.isf)|*.isf";

            var result = dlg.ShowDialog();
            if (result == true)
            {
                try
                {
                    var file = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read);
                    _inkCanvas.Strokes = new StrokeCollection(file);
                    file.Close();
                }
                catch (Exception)
                {

                }
            }
        }

        private void SaveInkButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();

            dlg.DefaultExt = ".isf";
            dlg.Filter = "ISF files (*.isf)|*.isf";

            var result = dlg.ShowDialog();
            if (result == true)
            {
                try
                {
                    var file = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write);
                    _inkCanvas.Strokes.Save(file);
                    file.Close();
                }
                catch (Exception)
                {

                }
            }
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

                _mainWindow.RobotArm.ArmDown(false);
            }
        }
    }
}
