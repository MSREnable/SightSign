using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using Microsoft.EyeGaze.Mouse;

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

            // Factors used to generate data to send to the robot.

            RobotArm = new RobotArm(xScreen / 2.0, yScreen / 2.0, Math.Min(xScreen, yScreen) / 2.0, inkCanvas, canvas);
            //            this.DataContext = this.RobotArm;

            _settings = new Settings(RobotArm);
            _settings.LoadSettings();

            DataContext = _settings;

            Background = new SolidColorBrush(_settings.BackgroundColor);
            // inkCanvas.Background = new SolidColorBrush(_settings.BackgroundColor);

            if (_settings.RobotControl)
            {
                RobotArm.Enabled = true;
                RobotArm.Connect();
            }

            LoadInk();

            ApplySettingsToInk();

            // Lift the arm.
            RobotArm.ArmDown(false);
        }

        // Load the ink from the persisted filename.
        internal void LoadInk()
        {
            var filename = Settings1.Default.LoadedInkLocation;
            if (string.IsNullOrEmpty(filename))
            {
                var defaultFile = AppDomain.CurrentDomain.BaseDirectory + "Signature.xml";
                if (File.Exists(defaultFile))
                {
                    filename = defaultFile;
                }
            }

            if (!string.IsNullOrEmpty(filename))
            {
                LoadInkFromXmlFile(filename);
            }
        }

        // Apply current settings to the current ink.
        internal void ApplySettingsToInk()
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

        private bool _stampInProgress;

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

        internal void ResetWriting()
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

        internal void LoadInkFromXmlFile(string filename)
        {
            var collection = LoadXml(filename);

            double baseLength = 0;

            for (var idx = 0; idx < collection.Count; ++idx)
            {
                var pathGeometry = collection[idx];

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

            // If no matrix has been applied to the ink, center and scale it automatically.
            if (Settings1.Default.InkMatrix == "")
            {
                CenterInkOnScreen();
            }
        }

        private void CenterInkOnScreen()
        {
            // Get the bounding rect of the new ink.
            if (inkCanvas.Strokes.Count > 0)
            {
                var boundingRect = new Rect();

                foreach (var stroke in inkCanvas.Strokes)
                {
                    // TODO consider changing to tolerance comparison, since this is double
                    if ((boundingRect.Size.Width == 0) &&
                        (boundingRect.Size.Height == 0))
                    {
                        boundingRect = stroke.GetBounds();
                    }
                    else
                    {
                        boundingRect.Union(stroke.GetBounds());
                    }
                }

                // TODO consider changing to tolerance comparison, since this is double
                if ((boundingRect.Size.Width != 0) &&
                    (boundingRect.Size.Height != 0))
                {
                    // The app has yet to be shown on the screen, and the dimensions of the window or
                    // its elements are not yet available to us. So assume that the app will fill the
                    // working area of the screen, and the should fill most of the app.

                    // Buffer allows some gap between the ink and the edge of the app.
                    const double buffer = 160.0;

                    var screenWidth = SystemParameters.WorkArea.Width - buffer;
                    var screenHeight = SystemParameters.WorkArea.Height - buffer;

                    var scaleFactor = Math.Min(
                        screenWidth / boundingRect.Width,
                        screenHeight / boundingRect.Height);

                    // Firtst scale up the ink.
                    var matrix = new Matrix();

                    matrix.Scale(scaleFactor, scaleFactor);

                    // Now shift the ink to be more in the center of the screen.
                    var centerPtOriginal = new Point(
                        boundingRect.Left + (boundingRect.Width / 2),
                        boundingRect.Top + (boundingRect.Height / 2));

                    var centerPtScaled = new Point(
                        centerPtOriginal.X * scaleFactor,
                        centerPtOriginal.Y * scaleFactor);

                    matrix.Translate(
                        (screenWidth / 2.0) - centerPtScaled.X + (buffer / scaleFactor) - 80,
                        (screenHeight / 2.0) - centerPtScaled.Y + (buffer / scaleFactor) - 20);

                    _settings.InkMatrix = matrix;

                    // By persisting the matrix here, it will be applied shortly.
                    Settings1.Default.InkMatrix = _settings.InkMatrix.ToString();
                    Settings1.Default.Save();
                }
            }
        }

        private static ObservableCollection<PathGeometry> LoadXml(string filename)
        {
            var xml = XDocument.Load(filename);

            var collection = new ObservableCollection<PathGeometry>();

            foreach (var e in xml.Descendants("Path"))
            {
                var xAttribute = e.Attribute("Data");
                if (xAttribute != null)
                {
                    var path = xAttribute.Value;

                    var geometry = Geometry.Parse(path);

                    var pathGeometry = geometry.GetFlattenedPathGeometry();

                    collection.Add(pathGeometry);
                }
            }

            return collection;
        }

        public static double GetLength(PathGeometry pathGeometry)
        {
            var length = 0.0;

            foreach (var pf in pathGeometry.Figures)
            {
                var start = pf.StartPoint;

                foreach (var pathSegment in pf.Segments)
                {
                    var seg = (PolyLineSegment) pathSegment;
                    foreach (var point in seg.Points)
                    {
                        length += Distance(start, point);

                        start = point;
                    }
                }
            }

            return length;
        }

        private static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        private void StackPanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShowSettingsWindow();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.S: // settings
                    ShowSettingsWindow();
                    break;
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