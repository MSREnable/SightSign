using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.EyeGaze.Mouse;
using Microsoft.Win32;
using System.Diagnostics;
using System.Collections.Generic;

namespace eyeSign
{
    public partial class MainWindow
    {
        private bool _simpleButtonCanBeClicked = true;

        private Stroke _strokeBeingAnimated;
        private int _currentAnimatedStrokeIndex;
        private int _currentAnimatedPointIndex;
        private DispatcherTimer _dispatcherTimer;
        private bool _inTimer;

        private bool _stampInProgress;

        private DispatcherTimer _buttonEnabledTimer;

        public RobotArm RobotArm { get; }

        private Settings _settings;

        // Note: Reflecta (which i believe is being used) makes port stuff go away

        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;

            // Assume the screen size won't change after the app starts.
            var xScreen = SystemParameters.PrimaryScreenWidth;
            var yScreen = SystemParameters.PrimaryScreenHeight;

            RobotArm = new RobotArm(
                xScreen / 2.0, 
                yScreen / 2.0, 
                Math.Min(xScreen, yScreen) / 2.0, 
                inkCanvas, 
                canvas);

            _settings = new Settings(RobotArm);
            _settings.LoadSettings();

            DataContext = _settings;

            Background = new SolidColorBrush(_settings.BackgroundColor);

            if (_settings.RobotControl)
            {
                RobotArm.Enabled = true;
                RobotArm.Connect();
            }

            inkCanvas.DefaultDrawingAttributes.Color = _settings.InkColor;
            inkCanvas.DefaultDrawingAttributes.Width = _settings.InkWidth;
            inkCanvas.DefaultDrawingAttributes.Height = _settings.InkWidth;

            LoadInkOnStartup();

            // Lift the arm.
            RobotArm.ArmDown(false);
        }

        private void LoadInkOnStartup()
        {
            var filename = Settings1.Default.LoadedInkLocation;
            if (string.IsNullOrEmpty(filename))
            {
                filename = AppDomain.CurrentDomain.BaseDirectory + "Signature.isf";
            }

            if (File.Exists(filename))
            {
                AddInkFromFile(filename);
            }
        }

        private void AddInkFromFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return;
            }

            // Remove any existing ink first.
            inkCanvas.Strokes.Clear();

            StrokeCollection strokeCollection = new StrokeCollection();

            // Assume the file is valid and accessible.
            var file = new FileStream(filename, FileMode.Open, FileAccess.Read);
            strokeCollection = new StrokeCollection(file);
            file.Close();

            if (strokeCollection.Count > 0)
            {
                GenerateStrokesWithEvenlyDistributedPoints(strokeCollection);

                ApplySettingsToInk();
            }
        }

        // Apply current settings to the current ink.
        private void ApplySettingsToInk()
        {
            if (inkCanvas.Strokes.Count > 0)
            {
                foreach (var stroke in inkCanvas.Strokes)
                {
                    stroke.Transform(_settings.InkMatrix, false);

                    stroke.DrawingAttributes.Color = _settings.InkColor;
                    stroke.DrawingAttributes.Width = _settings.InkWidth;
                    stroke.DrawingAttributes.Height = _settings.InkWidth;
                }
            }
        }

        // App's closing down.
        protected override void OnClosed(EventArgs e)
        {
            // Now disconnect the arm.
            RobotArm.Close();

            base.OnClosed(e);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Note that we're not interrupting the robot here.

            ResetWriting();

            if (StampButton.Visibility == Visibility.Visible)
            {
                EditButton.Content = "Done";

                StampButton.Visibility = Visibility.Collapsed;
                WriteButton.Visibility = Visibility.Collapsed;

                ClearButton.Visibility = Visibility.Visible;
                SaveButton.Visibility = Visibility.Visible;
                LoadButton.Visibility = Visibility.Visible;

                inkCanvas.IsEnabled = true;
            }
            else
            {
                EditButton.Content = "Edit";

                StampButton.Visibility = Visibility.Visible;
                WriteButton.Visibility = Visibility.Visible;

                ClearButton.Visibility = Visibility.Collapsed;
                SaveButton.Visibility = Visibility.Collapsed;
                LoadButton.Visibility = Visibility.Collapsed;

                inkCanvas.IsEnabled = false;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsWindow();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.Strokes.Clear();
            inkCanvasAnimations.Strokes.Clear();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
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
                    inkCanvas.Strokes.Save(file);
                    file.Close();

                    Settings1.Default.LoadedInkLocation = dlg.FileName;
                    Settings1.Default.Save();
                }
                catch (Exception)
                {

                }
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();

            dlg.DefaultExt = ".isf";
            dlg.Filter = "ISF files (*.isf)|*.isf";

            var result = dlg.ShowDialog();
            if (result == true)
            {
                AddInkFromFile(dlg.FileName);

                Settings1.Default.LoadedInkLocation = dlg.FileName;
                Settings1.Default.Save();
            }
        }

        // Send the entire signature to the robot, with no app visuals updated.
        private void StampButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_simpleButtonCanBeClicked)
            {
                return;
            }

            StartButtonEnabledTimer();

            // Stop any in-progress writing.
            ResetWriting();

            _stampInProgress = true;

            WriteSignature();
        }

        // Write the signature visually, and send the data to the robot.
        private void WriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_simpleButtonCanBeClicked)
            {
                return;
            }

            StartButtonEnabledTimer();

            // Stop any in-progress writing.
            ResetWriting();

            WriteSignature();
        }

        private void WriteSignature()
        {
            if (inkCanvas.Strokes.Count == 0)
            {
                return;
            }

            // If writing is already in progress, do nothing.
            if (_dispatcherTimer != null)
            {
                return;
            }

            dot.Opacity = 1.0;

            // Hide the dot if its width is zero.
            dot.Visibility = (dot.Width > 0 ? Visibility.Visible : Visibility.Collapsed);
            inkCanvasAnimations.Visibility = Visibility.Visible;

            inkCanvasAnimations.Strokes.Clear();

            _currentAnimatedPointIndex = 0;
            _currentAnimatedStrokeIndex = 0;

            // Apply a fade to the ink, (which might mean making it completely invisible.)
            foreach (var stroke in inkCanvas.Strokes)
            {
                stroke.DrawingAttributes.Color = _settings.FadedInkColor;
            }

            // Lift arm up.
            RobotArm.ArmDown(false);

            // Move to the start of the signature.
            var stylusPointFirst = inkCanvas.Strokes[0].StylusPoints[0];
            MoveDotAndRobotToStylusPoint(stylusPointFirst);

            // We'll create the animation stroke after the first interval.
            _strokeBeingAnimated = null;

            // Begin the timer used for animations.
            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += dispatcherTimer_Tick;
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, _settings.AnimationInterval);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetWriting();
        }

        private void ResetWriting()
        {
            _stampInProgress = false;

            if (_dispatcherTimer != null)
            {
                _dispatcherTimer.Stop();
                _dispatcherTimer = null;
            }

            _currentAnimatedPointIndex = 0;
            _currentAnimatedStrokeIndex = 0;

            inkCanvasAnimations.Strokes.Clear();
            inkCanvasAnimations.Visibility = Visibility.Collapsed;

            dot.Visibility = Visibility.Collapsed;

            foreach (var stroke in inkCanvas.Strokes)
            {
                stroke.DrawingAttributes.Color = _settings.InkColor;
            }
        }

        private void MoveDotAndRobotToStylusPoint(StylusPoint stylusPt)
        {
            var pt = stylusPt.ToPoint();

            if (dot.Visibility == Visibility.Visible)
            {
                dotTranslateTransform.X = pt.X - (inkCanvas.ActualWidth / 2);
                dotTranslateTransform.Y = pt.Y - (inkCanvas.ActualHeight / 2);
            }

            // Send the point to the robot too.
            RobotArm.Move(pt);
        }

        private void AddFirstPointToNewStroke(StylusPoint pt)
        {
            // Create a new stroke for the continuing animation.
            var ptCollection = new StylusPointCollection();
            ptCollection.Add(pt);

            _strokeBeingAnimated = new Stroke(ptCollection);

            _strokeBeingAnimated.DrawingAttributes.Color = _settings.InkColor;
            _strokeBeingAnimated.DrawingAttributes.Width = _settings.InkWidth;
            _strokeBeingAnimated.DrawingAttributes.Height = _settings.InkWidth;
            _strokeBeingAnimated.DrawingAttributes.StylusTip = StylusTip.Ellipse;

            inkCanvasAnimations.Strokes.Add(_strokeBeingAnimated);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_inTimer)
            {
                return;
            }

            _inTimer = true;

            // Have we created a new stroke for this animation yet?
            if (_strokeBeingAnimated == null)
            {
                var firstPt = inkCanvas.Strokes[_currentAnimatedStrokeIndex].StylusPoints[0];

                RobotArm.ArmDown(true);

                AddFirstPointToNewStroke(firstPt);
            }

            ++_currentAnimatedPointIndex;

            // Have we reached the end of a stroke?
            if (_currentAnimatedPointIndex >=
                inkCanvas.Strokes[_currentAnimatedStrokeIndex].StylusPoints.Count)
            {
                // In "Click dot to write" mode, (and not dropping manual ink,) don't wait at the end of very short strokes.
                var shortStroke = (_currentAnimatedPointIndex < 3);

                if (_stampInProgress || shortStroke)
                {
                    // Next animation will be at the start of some stroke.
                    _currentAnimatedPointIndex = 0;

                    // Move to the next stroke.
                    ++_currentAnimatedStrokeIndex;

                    // Do we have more strokes to write?
                    if (_currentAnimatedStrokeIndex < inkCanvas.Strokes.Count)
                    {
                        MoveToNextStroke();

                        if (!_stampInProgress)
                        {
                            dot.Opacity = 1.0;
                        }

                        if (!_stampInProgress && shortStroke)
                        {
                            LiftArmAndStopAnimationTimer();
                        }
                    }
                    else
                    {
                        // We've reached the end of the last stroke.
                        _currentAnimatedStrokeIndex = 0;

                        dot.Visibility = Visibility.Collapsed;

                        LiftArmAndStopAnimationTimer();

                        _dispatcherTimer = null;
                    }
                }
                else
                {
                    // Click dot to write is enabled. So stop the timer until the dot is clicked.
                    LiftArmAndStopAnimationTimer();

                    // If we've not reached end of the last stroke, Show an opaque dot 
                    // to indicate that it's waiting to be clicked.
                    if (_currentAnimatedStrokeIndex < inkCanvas.Strokes.Count - 1)
                    {
                        if (!_stampInProgress)
                        {
                            dot.Opacity = 1.0;
                        }
                    }
                    else
                    {
                        _stampInProgress = false;

                        dot.Visibility = Visibility.Collapsed;

                        _dispatcherTimer = null;
                    }
                }
            }
            else
            {
                // We're continuing to animate the stroke we'we were already on.

                var stylusPt = inkCanvas.Strokes[_currentAnimatedStrokeIndex].StylusPoints[_currentAnimatedPointIndex];
                var stylusPtPrevious = inkCanvas.Strokes[_currentAnimatedStrokeIndex].StylusPoints[_currentAnimatedPointIndex - 1];

                // Keep looking for a point sufficiently far from the point that the dot's currently at.
                var threshold = 1;

                while ((Math.Abs(stylusPt.X - stylusPtPrevious.X) < threshold) &&
                        (Math.Abs(stylusPt.Y - stylusPtPrevious.Y) < threshold))
                {
                    ++_currentAnimatedPointIndex;

                    if (_currentAnimatedPointIndex >= inkCanvas.Strokes[_currentAnimatedStrokeIndex].StylusPoints.Count)
                    {
                        break;
                    }

                    stylusPt = inkCanvas.Strokes[_currentAnimatedStrokeIndex].StylusPoints[_currentAnimatedPointIndex];
                }

                // Leave the arm in its current down state.

                MoveDotAndRobotToStylusPoint(stylusPt);

                _strokeBeingAnimated?.StylusPoints.Add(stylusPt);
            }

            _inTimer = false;
        }

        private void LiftArmAndStopAnimationTimer()
        {
            _dispatcherTimer.Stop();

            RobotArm.ArmDown(false);
        }

        private void MoveToNextStroke()
        {
            // Move to the next stroke.
            var stylusPtNext =
                inkCanvas.Strokes[_currentAnimatedStrokeIndex].StylusPoints[_currentAnimatedPointIndex];

            // We'll create the animation stroke after the first interval.
            _strokeBeingAnimated = null;

            // Lift the arm up before moving the dot to the start of the next stroke.
            RobotArm.ArmDown(false);

            MoveDotAndRobotToStylusPoint(stylusPtNext);
        }

        private void GenerateStrokesWithEvenlyDistributedPoints(StrokeCollection strokeCollection)
        {
            double baseLength = 0;

            for (var idx = 0; idx < strokeCollection.Count; ++idx)
            {
                StylusPointCollection existingStylusPoints = strokeCollection[idx].StylusPoints;
                if (existingStylusPoints.Count > 0)
                {
                    Point start = existingStylusPoints[0].ToPoint();

                    List<LineSegment> segments = new List<LineSegment>();

                    for (int i = 1; i < existingStylusPoints.Count; i++)
                    {
                        segments.Add(new LineSegment(existingStylusPoints[i].ToPoint(), true));
                    }

                    PathFigure figure = new PathFigure(start, segments, false /* Closed */ );
                    PathGeometry pathGeometry = new PathGeometry();
                    pathGeometry.Figures.Add(figure);

                    var currentLength = GetLength(pathGeometry);

                    if (idx == 0)
                    {
                        baseLength = currentLength;
                    }

                    // Each stroke will have the required number of points to keep the animation
                    // speed roughly the same for all strokes. Always have at least two points on
                    // the stroke.
                    var count = Math.Max(2, (int)((_settings.AnimationPointsOnFirstStroke * currentLength) / baseLength));

                    var stylusPoints = new StylusPointCollection();

                    for (var i = 0; i < count; ++i)
                    {
                        var distanceFraction = i / (double)count;

                        Point pt;
                        Point ptTangent;
                        pathGeometry.GetPointAtFractionLength(
                            distanceFraction, out pt, out ptTangent);

                        stylusPoints.Add(new StylusPoint(pt.X, pt.Y));
                    }

                    if (stylusPoints.Count > 0)
                    {
                        var stroke = new Stroke(stylusPoints);
                        inkCanvas.Strokes.Add(stroke);
                    }
                }
            }
        }
        
        public static double GetLength(PathGeometry pathGeometry)
        {
            var length = 0.0;

            foreach (var pf in pathGeometry.Figures)
            {
                var start = pf.StartPoint;

                foreach (var pathSegment in pf.Segments)
                {
                    LineSegment lineSegment = pathSegment as LineSegment;
                    if (lineSegment != null)
                    {
                        length += Distance(start, lineSegment.Point);

                        start = lineSegment.Point;
                    }
                    else
                    {
                        PolyLineSegment polylineSegment = pathSegment as PolyLineSegment;
                        if (polylineSegment != null)
                        {
                            foreach (var point in polylineSegment.Points)
                            {
                                length += Distance(start, point);

                                start = point;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Unexpected data - Segment is neither LineSegment or PolylineSegment.");
                        }
                    }
                }
            }

            return length;
        }

        private static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.D: // arm down
                    RobotArm.ArmDown(true);
                    break;
                case Key.U: // arm up
                    RobotArm.ArmDown(false);
                    break;
                case Key.H: // arm up
                    RobotArm.Home();
                    break;
                case Key.C:
                    RobotArm.CircleTest();
                    break;
            }
        }

        private void ShowSettingsWindow()
        {
            var settingsWindow = new SettingsWindow(this, _settings, RobotArm, inkCanvas, inkCanvasAnimations);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            RobotArm.ZShift += Math.Sign(e.Delta) * 0.01;
        }

        private void MoveDot()
        {
            if (_dispatcherTimer != null)
            {
                // Only react to the mouse down on the dot if the the timer's 
                // not currently running.
                if (!_dispatcherTimer.IsEnabled)
                {
                    // Are we at the end of a stroke?
                    if (_currentAnimatedPointIndex >=
                        inkCanvas.Strokes[_currentAnimatedStrokeIndex].StylusPoints.Count - 1)
                    {
                        // Raise the robot arm at the end of the stroke.
                        RobotArm.ArmDown(false);

                        // We're at the end of stroke. If this isn't the last stroke, move to the next stroke.
                        if (_currentAnimatedStrokeIndex < inkCanvas.Strokes.Count - 1)
                        {
                            // Move to the start of the next stroke.
                            _currentAnimatedPointIndex = 0;

                            // Move to the next stroke.
                            ++_currentAnimatedStrokeIndex;

                            MoveToNextStroke();
                        }
                    }
                    else
                    {
                        // Start animating the dot.
                        _dispatcherTimer.Start();
                                                                    
                        // Show a translucent dot while it's being animated.
                        if (!SystemParameters.HighContrast)
                        {
                            dot.Opacity = 0.5;
                        }
                    }
                }
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            GazeMouse.Attach(this);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            GazeMouse.DetachAll();
        }

        private void Dot_OnClick(object sender, RoutedEventArgs e)
        {
            MoveDot();
        }

        private void StartButtonEnabledTimer()
        {
            _simpleButtonCanBeClicked = false;

            _buttonEnabledTimer = new DispatcherTimer();
            _buttonEnabledTimer.Tick += buttonEnabledTimer_Tick;
            _buttonEnabledTimer.Interval = new TimeSpan(0, 0, 0, 10);
            _buttonEnabledTimer.Start();
        }

        private void buttonEnabledTimer_Tick(object sender, EventArgs e)
        {
            _buttonEnabledTimer.Stop();
            _buttonEnabledTimer = null;

            _simpleButtonCanBeClicked = true;
        }
    }

    public class ColorToSolidBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = (Color)value;

            return new SolidColorBrush(Color.FromArgb(
                color.A, color.R, color.G, color.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ArmStateToDotFillConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var armDown = (bool)value;

            var color = armDown ?
                Settings1.Default.DotDownColor : Settings1.Default.DotColor;

            return new SolidColorBrush(Color.FromArgb(
                color.A, color.R, color.G, color.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ArmConnectedToContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var armConnected = (bool)value;

            return "\uE99A" + (armConnected ? "\uE10B" : "\uE10A");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ArmConnectedToHelpTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var armConnected = (bool)value;

            return "Robot " + (armConnected ? "connected" : "disconnected");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ArmStateToDotWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var armDown = (bool)value;

            return (armDown ? Settings1.Default.DotDownWidth : Settings1.Default.DotWidth);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}