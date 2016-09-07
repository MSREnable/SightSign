using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
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

            BackgroundColorTextBox.Text = _settings.BackgroundColor.ToString();
            ButtonBackgroundColorTextBox.Text = _settings.ButtonBackgroundColor.ToString();
            ButtonTextColorTextBox.Text = _settings.ButtonTextColor.ToString();
            ButtonBorderColorTextBox.Text = _settings.ButtonBorderColor.ToString();

            var buttonBorderWidth = _settings.ButtonBorderWidth;
            ButtonBorderWidthTextBox.Text = buttonBorderWidth.ToString();

            InkColorTextBox.Text = _settings.InkColor.ToString();

            var inkWidth = _settings.InkWidth;
            InkWidthTextBox.Text = inkWidth.ToString();

            var animationInterval = _settings.AnimationInterval;
            AnimationIntervalTextBox.Text = animationInterval.ToString();

            var animationPointsOnFirstStroke = _settings.AnimationPointsOnFirstStroke;
            AnimationPointsOnFirstStrokeTextBox.Text = animationPointsOnFirstStroke.ToString();

            FadedInkColorTextBox.Text = _settings.FadedInkColor.ToString();

            DotColorTextBox.Text = _settings.DotColor.ToString();

            DotDownColorTextBox.Text = _settings.DotDownColor.ToString();

            var dotWidth = _settings.DotWidth;
            DotWidthTextBox.Text = dotWidth.ToString();

            var dotDownWidth = _settings.DotDownWidth;
            DotDownWidthTextBox.Text = dotDownWidth.ToString();
        }

        private void InkColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var inkString = InkColorTextBox.Text;

            try
            {
                _settings.InkColor = (Color)ColorConverter.ConvertFromString(inkString);

                foreach (var stroke in _inkCanvas.Strokes)
                {
                    stroke.DrawingAttributes.Color = _settings.InkColor;
                }

                var inkColorSetting = System.Drawing.Color.FromArgb(
                    _settings.InkColor.A, _settings.InkColor.R, _settings.InkColor.G, _settings.InkColor.B);

                Settings1.Default.InkColor = inkColorSetting;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void InkWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var widthString = InkWidthTextBox.Text;

            try
            {
                _settings.InkWidth = Int32.Parse(widthString);

                foreach (var stroke in _inkCanvas.Strokes)
                {
                    stroke.DrawingAttributes.Width = _settings.InkWidth;
                    stroke.DrawingAttributes.Height = _settings.InkWidth;
                }

                Settings1.Default.InkWidth = _settings.InkWidth;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void BackgroundColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var colorString = BackgroundColorTextBox.Text;

            try
            {
                _settings.BackgroundColor = (Color)ColorConverter.ConvertFromString(colorString);

                _mainWindow.Background = new SolidColorBrush(_settings.BackgroundColor);
                // _mainWindow.inkCanvas.Background = new SolidColorBrush(_settings.BackgroundColor);

                var backgroundColorSetting = System.Drawing.Color.FromArgb(
                    _settings.BackgroundColor.A, 
                    _settings.BackgroundColor.R, 
                    _settings.BackgroundColor.G, 
                    _settings.BackgroundColor.B);

                Settings1.Default.BackgroundColor = backgroundColorSetting;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void ButtonBackgroundColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var colorString = ButtonBackgroundColorTextBox.Text;

            try
            {
                _settings.ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString(colorString);

                var buttonBackgroundColorSettings = System.Drawing.Color.FromArgb(
                    _settings.ButtonBackgroundColor.A,
                    _settings.ButtonBackgroundColor.R,
                    _settings.ButtonBackgroundColor.G,
                    _settings.ButtonBackgroundColor.B);

                Settings1.Default.ButtonBackgroundColor = buttonBackgroundColorSettings;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void ButtonTextColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var colorString = ButtonTextColorTextBox.Text;

            try
            {
                _settings.ButtonTextColor = (Color)ColorConverter.ConvertFromString(colorString);

                var buttonTextColorSettings = System.Drawing.Color.FromArgb(
                    _settings.ButtonTextColor.A,
                    _settings.ButtonTextColor.R,
                    _settings.ButtonTextColor.G,
                    _settings.ButtonTextColor.B);

                Settings1.Default.ButtonTextColor = buttonTextColorSettings;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void ButtonBorderColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var colorString = ButtonBorderColorTextBox.Text;

            try
            {
                _settings.ButtonBorderColor = (Color)ColorConverter.ConvertFromString(colorString);

                var buttonBorderColorSettings = System.Drawing.Color.FromArgb(
                    _settings.ButtonBorderColor.A,
                    _settings.ButtonBorderColor.R,
                    _settings.ButtonBorderColor.G,
                    _settings.ButtonBorderColor.B);

                Settings1.Default.ButtonBorderColor = buttonBorderColorSettings;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void ButtonBorderWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var widthString = ButtonBorderWidthTextBox.Text;

            try
            {
                _settings.ButtonBorderWidth = Int32.Parse(widthString);

                Settings1.Default.ButtonBorderWidth = _settings.ButtonBorderWidth;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void FadedInkColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var inkString = FadedInkColorTextBox.Text;

            try
            {
                _settings.FadedInkColor = (Color)ColorConverter.ConvertFromString(inkString);

                var fadedInkColorSetting = System.Drawing.Color.FromArgb(
                    _settings.FadedInkColor.A, _settings.FadedInkColor.R, _settings.FadedInkColor.G, _settings.FadedInkColor.B);

                Settings1.Default.FadedInkColor = fadedInkColorSetting;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void DotColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var colString = DotColorTextBox.Text;

            try
            {
                _settings.DotColor = (Color)ColorConverter.ConvertFromString(colString);

                var dotColor = System.Drawing.Color.FromArgb(
                    _settings.DotColor.A, _settings.DotColor.R, _settings.DotColor.G, _settings.DotColor.B);

                Settings1.Default.DotColor = dotColor;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void DotDownColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var colString = DotDownColorTextBox.Text;

            try
            {
                _settings.DotDownColor = (Color)ColorConverter.ConvertFromString(colString);

                var dotColor = System.Drawing.Color.FromArgb(
                    _settings.DotDownColor.A, 
                    _settings.DotDownColor.R,
                    _settings.DotDownColor.G, 
                    _settings.DotDownColor.B);

                Settings1.Default.DotDownColor = dotColor;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void DotWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var widthString = DotWidthTextBox.Text;

            try
            {
                _settings.DotWidth = Int32.Parse(widthString);

                Settings1.Default.DotWidth = _settings.DotWidth;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void DotDownWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var widthString = DotDownWidthTextBox.Text;

            try
            {
                _settings.DotDownWidth = Int32.Parse(widthString);

                Settings1.Default.DotDownWidth = _settings.DotDownWidth;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void AnimationInterval_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var intervalString = AnimationIntervalTextBox.Text;

            try
            {
                _settings.AnimationInterval = Int32.Parse(intervalString);

                Settings1.Default.AnimationInterval = _settings.AnimationInterval;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
        }

        private void AnimationPointsOnFirstStroke_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_windowIntialized)
            {
                return;
            }

            var pointsString = AnimationPointsOnFirstStrokeTextBox.Text;

            try
            {
                _settings.AnimationPointsOnFirstStroke = Int32.Parse(pointsString);

                Settings1.Default.AnimationPointsOnFirstStroke = _settings.AnimationPointsOnFirstStroke;
                Settings1.Default.Save();
            }
            catch (Exception)
            {

            }
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

        private void MoveInkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_inkCanvas.Strokes.Count == 0)
            {
                return;
            }

            var xShift = 0.0;
            var yShift = 0.0;

            var btn = sender as Button;

            switch (btn.Name)
            {
                case "MoveInkLeft":
                    xShift = -10.0;
                    break;

                case "MoveInkRight":
                    xShift = 10.0;
                    break;

                case "MoveInkUp":
                    yShift = -10.0;
                    break;

                case "MoveInkDown":
                    yShift = 10.0;
                    break;
            }

            var matrix = new Matrix();
            matrix.Translate(xShift, yShift);

            foreach (var stroke in _inkCanvas.Strokes)
            {
                stroke.Transform(matrix, false);
            }

            _settings.InkMatrix.Translate(xShift, yShift);

            Settings1.Default.InkMatrix = _settings.InkMatrix.ToString();
            Settings1.Default.Save();
        }

        private void ScaleInkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_inkCanvas.Strokes.Count == 0)
            {
                return;
            }

            var scale = 0.0;

            var btn = sender as Button;

            switch (btn.Name)
            {
                case "ScaleInkUp":
                    scale = 1.1;
                    break;

                case "ScaleInkDown":
                    scale = 0.9;
                    break;
            }

            var matrix = new Matrix();
            matrix.Scale(scale, scale);

            foreach (var stroke in _inkCanvas.Strokes)
            {
                stroke.Transform(matrix, false);
            }

            _settings.InkMatrix.Scale(scale, scale);

            Settings1.Default.InkMatrix = _settings.InkMatrix.ToString();
            Settings1.Default.Save();
        }

        private void ReloadInk_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ResetWriting();

            _inkCanvas.Strokes.Clear();

            _mainWindow.LoadInk();

            _mainWindow.ApplySettingsToInk();
        }

        private void ResetAllButton_Click(object sender, RoutedEventArgs e)
        {
            _inkCanvas.Strokes.Clear();

            _inkCanvasAnimations.Strokes.Clear();
            _inkCanvasAnimations.Visibility = Visibility.Collapsed;

            _settings.InkMatrix = new Matrix();

            // Reset to defaults. TODO: Don't have these defaults hard-coded here.

            BackgroundColorTextBox.Text = "#FFFFFFFF";
            ButtonBackgroundColorTextBox.Text = "#FFFFFFFF";
            ButtonTextColorTextBox.Text = "#FF000000";
            ButtonBorderColorTextBox.Text = "#FF000000";
            ButtonBorderWidthTextBox.Text = "2";

            InkWidthTextBox.Text = "4";
            InkColorTextBox.Text = "#FF000000";

            FadedInkColorTextBox.Text = "#20000000";

            DotColorTextBox.Text = "#8000FF00";
            DotWidthTextBox.Text = "20";

            DotDownColorTextBox.Text = "#FF00FF00";
            DotDownWidthTextBox.Text = "40";

            AnimationIntervalTextBox.Text = "20";
            AnimationPointsOnFirstStrokeTextBox.Text = "200";

            Settings1.Default.LoadedInkLocation = "";
            Settings1.Default.InkMatrix = "";
            Settings1.Default.Save();
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
