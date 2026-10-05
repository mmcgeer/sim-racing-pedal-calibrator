using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SimRacingPedalCalibrator.Models;
using System;
using Windows.Foundation;

namespace SimRacingPedalCalibrator
{
    public sealed partial class AxisCalibrationControl : UserControl
    {
        private AxisCalibration _calibration = new();
        private int _minSampled = int.MaxValue;
        private int _maxSampled = int.MinValue;
        
        // 10-point curve system
        private Point[] _curvePoints = new Point[11]; // 0 to 10 for 11 points
        private int _draggingPointIndex = -1;
        private const double POINT_RADIUS = 6;
        private const int NUM_POINTS = 11; // 0-10 inclusive

        public AxisCalibrationControl()
        {
            this.InitializeComponent();
            Loaded += AxisCalibrationControl_Loaded;
        }

        private void AxisCalibrationControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize 10-point curve to linear
                for (int i = 0; i < NUM_POINTS; i++)
                {
                    _curvePoints[i] = new Point(i / 10.0, i / 10.0);
                }
                
                // Add mouse event handlers to canvas
                CurveCanvas.PointerPressed += CurveCanvas_PointerPressed;
                CurveCanvas.PointerMoved += CurveCanvas_PointerMoved;
                CurveCanvas.PointerReleased += CurveCanvas_PointerReleased;
                
                // Draw initial curve
                DrawCurve();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AxisCalibrationControl_Loaded error: {ex}");
            }
        }

        public string AxisName
        {
            set => AxisNameText.Text = value;
        }

        public string AxisColor
        {
            set => ValueBar.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 
                byte.Parse(value.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(value.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(value.Substring(5, 2), System.Globalization.NumberStyles.HexNumber)));
        }

        public void SetCalibration(AxisCalibration calibration)
        {
            try
            {
                _calibration = calibration;
                MinInput.Value = calibration.Min;
                MaxInput.Value = calibration.Max;
                DeadZoneSlider.Value = calibration.DeadZone;
                
                // Copy curve points from calibration
                for (int i = 0; i < NUM_POINTS && i < calibration.CurvePoints.Length; i++)
                {
                    _curvePoints[i] = new Point(i / 10.0, calibration.CurvePoints[i]);
                }
                
                UpdateDisplay();
                DrawCurve();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetCalibration error: {ex}");
            }
        }

        public AxisCalibration GetCalibration()
        {
            try
            {
                int min = (int)MinInput.Value;
                int max = (int)MaxInput.Value;
                int center = (min + max) / 2;
                int deadZone = (int)DeadZoneSlider.Value;
                
                var calibration = new AxisCalibration(min, center, max, deadZone, CurveType.Linear, 1.0);
                
                // Copy current curve points
                for (int i = 0; i < NUM_POINTS && i < calibration.CurvePoints.Length; i++)
                {
                    calibration.CurvePoints[i] = _curvePoints[i].Y;
                }
                
                return calibration;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCalibration error: {ex}");
                // Return safe default
                int min = (int)MinInput.Value;
                int max = (int)MaxInput.Value;
                int center = (min + max) / 2;
                return new AxisCalibration(min, center, max, 0, CurveType.Linear, 1.0);
            }
        }

        public void SetRawValue(int rawValue)
        {
            CurrentValueText.Text = rawValue.ToString();
            ValueBar.Value = rawValue;
        }

        public void ResetCalibration()
        {
            _minSampled = int.MaxValue;
            _maxSampled = int.MinValue;
        }

        public void UpdateCalibrationData(int rawValue)
        {
            if (rawValue < _minSampled)
                _minSampled = rawValue;
            if (rawValue > _maxSampled)
                _maxSampled = rawValue;

            MinInput.Value = _minSampled;
            MaxInput.Value = _maxSampled;
        }

        private void UpdateDisplay()
        {
            try
            {
                MinValueText.Text = _calibration.Min.ToString();
                MaxValueText.Text = _calibration.Max.ToString();
                DeadZoneValue.Text = _calibration.DeadZone.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateDisplay error: {ex}");
            }
        }

        private void OnCalibrationValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs e)
        {
            UpdateDisplay();
        }

        private void OnDeadZoneChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            DeadZoneValue.Text = ((int)e.NewValue).ToString();
        }

        private void CurveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawCurve();
        }

        private void DrawCurve()
        {
            try
            {
                if (CurveCanvas.ActualWidth < 50 || CurveCanvas.ActualHeight < 50)
                    return;

                CurveCanvas.Children.Clear();

                double width = CurveCanvas.ActualWidth;
                double height = CurveCanvas.ActualHeight;
                double padding = 10;
                double graphWidth = width - (padding * 2);
                double graphHeight = height - (padding * 2);

                // Draw grid
                var gridBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 128, 128, 128));
                for (int i = 0; i <= 10; i++)
                {
                    double x = padding + (graphWidth / 10) * i;
                    double y = padding + (graphHeight / 10) * i;

                    var vLine = new Line { X1 = x, Y1 = padding, X2 = x, Y2 = padding + graphHeight, Stroke = gridBrush, StrokeThickness = 0.5 };
                    var hLine = new Line { X1 = padding, Y1 = y, X2 = padding + graphWidth, Y2 = y, Stroke = gridBrush, StrokeThickness = 0.5 };

                    CurveCanvas.Children.Add(vLine);
                    CurveCanvas.Children.Add(hLine);
                }

                // Draw diagonal reference line (linear)
                var referenceBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 200, 200, 200));
                var refLine = new Line
                {
                    X1 = padding,
                    Y1 = padding + graphHeight,
                    X2 = padding + graphWidth,
                    Y2 = padding,
                    Stroke = referenceBrush,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                };
                CurveCanvas.Children.Add(refLine);

                // Draw smooth curve through points using Catmull-Rom interpolation
                var curveBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 180, 255));
                var curveGroup = new Polyline { Stroke = curveBrush, StrokeThickness = 2 };

                // Generate smooth curve
                for (double t = 0; t <= 1; t += 0.01)
                {
                    int segment = (int)(t * (NUM_POINTS - 1));
                    double localT = (t * (NUM_POINTS - 1)) - segment;

                    // Get 4 points for Catmull-Rom
                    int p0 = Math.Max(0, segment - 1);
                    int p1 = segment;
                    int p2 = Math.Min(NUM_POINTS - 1, segment + 1);
                    int p3 = Math.Min(NUM_POINTS - 1, segment + 2);

                    // Catmull-Rom interpolation
                    double y = CatmullRom(
                        _curvePoints[p0].Y, _curvePoints[p1].Y, 
                        _curvePoints[p2].Y, _curvePoints[p3].Y, 
                        localT);

                    double x = padding + (t * graphWidth);
                    double screenY = padding + graphHeight - (y * graphHeight);

                    curveGroup.Points.Add(new Point(x, screenY));
                }

                CurveCanvas.Children.Add(curveGroup);

                // Draw control points
                var pointBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 200, 255));
                var selectedBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 100, 100));

                for (int i = 0; i < NUM_POINTS; i++)
                {
                    double pointX = padding + (_curvePoints[i].X * graphWidth);
                    double pointY = padding + graphHeight - (_curvePoints[i].Y * graphHeight);

                    var circle = new Ellipse
                    {
                        Width = POINT_RADIUS * 2,
                        Height = POINT_RADIUS * 2,
                        Fill = i == _draggingPointIndex ? selectedBrush : pointBrush,
                        Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 255, 255, 255)),
                        StrokeThickness = 1
                    };

                    Canvas.SetLeft(circle, pointX - POINT_RADIUS);
                    Canvas.SetTop(circle, pointY - POINT_RADIUS);
                    CurveCanvas.Children.Add(circle);
                }

                // Draw axes
                var axisBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 255, 255, 255));
                var axisLine = new Line
                {
                    X1 = padding,
                    Y1 = padding + graphHeight,
                    X2 = padding + graphWidth,
                    Y2 = padding + graphHeight,
                    Stroke = axisBrush,
                    StrokeThickness = 1.5
                };
                CurveCanvas.Children.Add(axisLine);

                var yAxisLine = new Line
                {
                    X1 = padding,
                    Y1 = padding,
                    X2 = padding,
                    Y2 = padding + graphHeight,
                    Stroke = axisBrush,
                    StrokeThickness = 1.5
                };
                CurveCanvas.Children.Add(yAxisLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DrawCurve error: {ex}");
            }
        }

        private double CatmullRom(double p0, double p1, double p2, double p3, double t)
        {
            double t2 = t * t;
            double t3 = t2 * t;

            return 0.5 * (
                2.0 * p1 +
                (-p0 + p2) * t +
                (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2 +
                (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3
            );
        }

        private void CurveCanvas_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _draggingPointIndex = -1;
            e.Handled = true;
        }

        private void CurveCanvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(CurveCanvas);
            double x = point.Position.X;
            double y = point.Position.Y;

            double padding = 10;
            double graphWidth = CurveCanvas.ActualWidth - (padding * 2);
            double graphHeight = CurveCanvas.ActualHeight - (padding * 2);

            // Check if clicked on a control point
            for (int i = 0; i < NUM_POINTS; i++)
            {
                double pointX = padding + (_curvePoints[i].X * graphWidth);
                double pointY = padding + graphHeight - (_curvePoints[i].Y * graphHeight);

                double dx = x - pointX;
                double dy = y - pointY;
                if (Math.Sqrt(dx * dx + dy * dy) < POINT_RADIUS + 5)
                {
                    _draggingPointIndex = i;
                    e.Handled = true;
                    break;
                }
            }
        }

        private void CurveCanvas_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (_draggingPointIndex < 0) return;

            var point = e.GetCurrentPoint(CurveCanvas);
            double x = point.Position.X;
            double y = point.Position.Y;

            double padding = 10;
            double graphWidth = CurveCanvas.ActualWidth - (padding * 2);
            double graphHeight = CurveCanvas.ActualHeight - (padding * 2);

            // Constrain to graph area
            double newX = Math.Max(0, Math.Min(1, (x - padding) / graphWidth));
            double newY = Math.Max(0, Math.Min(1, (graphHeight - (y - padding)) / graphHeight));

            // First and last points locked to corners
            if (_draggingPointIndex == 0)
            {
                newX = 0;
                newY = 0;
            }
            else if (_draggingPointIndex == NUM_POINTS - 1)
            {
                newX = 1;
                newY = 1;
            }

            _curvePoints[_draggingPointIndex] = new Point(newX, newY);
            DrawCurve();
            e.Handled = true;
        }

        private void UpdateCurveDescription()
        {
            CurveDescription.Text = "10-point interactive curve - drag points to customize";
        }
    }
}
