using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SimRacingPedalCalibrator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Storage;

namespace SimRacingPedalCalibrator
{
    public sealed partial class AxisCalibrationControl : UserControl
    {
        private AxisCalibration _calibration = new();
        private int _minSampled = int.MaxValue;
        private int _maxSampled = int.MinValue;
        private bool _isUpdatingAxisSource;
        
        // 10-point curve system
        private Point[] _curvePoints = new Point[11]; // 0 to 10 for 11 points
        private int _draggingPointIndex = -1;
        private const double POINT_RADIUS = 6;
        private const int NUM_POINTS = 11; // 0-10 inclusive
        
        // Curve profiles storage
        private Dictionary<string, double[]> _curveProfiles = new();
        private string _currentAxisName = "";
        private const string PROFILES_KEY = "CurveProfiles";

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
                
                // Load saved profiles
                LoadAllProfiles();
                
                // Draw initial curve
                DrawCurve();
                UpdateCurveDeviation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AxisCalibrationControl_Loaded error: {ex}");
            }
        }

        public string AxisName
        {
            set 
            { 
                AxisNameText.Text = value;
                _currentAxisName = value;
                if (MappingTitleText != null)
                    MappingTitleText.Text = $"{value} Input Source";
            }
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
                MinInput.Minimum = calibration.Min;
                MinInput.Maximum = calibration.Max;
                MaxInput.Minimum = calibration.Min;
                MaxInput.Maximum = calibration.Max;
                ValueBar.Maximum = Math.Max(1, calibration.Max);
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
            
            // NEW: Calculate and display curve output
            try
            {
                int min = (int)MinInput.Value;
                int max = (int)MaxInput.Value;
                
                // Normalize raw value to 0-1 range
                double normalized = (rawValue - min) / (double)Math.Max(1, max - min);
                normalized = Math.Clamp(normalized, 0, 1);
                
                // Apply curve to get output
                double curveOutput = ApplyCurvePoints(normalized);
                
                // Update display
                int inputPercentage = (int)(normalized * 100);
                int outputPercentage = (int)(curveOutput * 100);
                
                RawInputValue.Text = $"{inputPercentage}%";
                CurveOutputValue.Text = $"{outputPercentage}%";
                CalibratedValueText.Text = $"{outputPercentage}%";
                CalibratedBar.Value = outputPercentage;
                InputProgressBar.Value = inputPercentage;
                OutputProgressBar.Value = outputPercentage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetRawValue curve output error: {ex}");
            }
        }

        public void SetFirmwareOutput(int? output)
        {
            FirmwareOutputText.Text = output?.ToString() ?? "n/a";
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

        // NEW: Evaluate curve output for a given input value (0-1)
        private double ApplyCurvePoints(double input)
        {
            if (input <= 0) return _curvePoints[0].Y;
            if (input >= 1) return _curvePoints[NUM_POINTS - 1].Y;

            // Find which segment the input falls into
            int segment = (int)(input * (NUM_POINTS - 1));
            double localT = (input * (NUM_POINTS - 1)) - segment;

            // Clamp segment to valid range
            segment = Math.Max(0, Math.Min(NUM_POINTS - 2, segment));

            // Get 4 points for Catmull-Rom interpolation
            int p0 = Math.Max(0, segment - 1);
            int p1 = segment;
            int p2 = Math.Min(NUM_POINTS - 1, segment + 1);
            int p3 = Math.Min(NUM_POINTS - 1, segment + 2);

            // Apply Catmull-Rom interpolation
            double output = CatmullRom(
                _curvePoints[p0].Y, _curvePoints[p1].Y,
                _curvePoints[p2].Y, _curvePoints[p3].Y,
                localT);

            // Clamp output to valid range
            return Math.Clamp(output, 0, 1);
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
            UpdateCurveDeviation();
            e.Handled = true;
        }

        private double CalculateCurveDeviation()
        {
            // Calculate average distance from linear curve
            double totalDeviation = 0;
            for (int i = 0; i < NUM_POINTS; i++)
            {
                double linearY = i / 10.0;
                double deviation = Math.Abs(_curvePoints[i].Y - linearY);
                totalDeviation += deviation;
            }
            return (totalDeviation / NUM_POINTS) * 100; // Convert to percentage
        }

        private void UpdateCurveDeviation()
        {
            double deviation = CalculateCurveDeviation();
            CurveDeviation.Text = $"Deviation: {deviation:F1}%";
        }

        // Preset Curves
        private void OnPresetRacingBrake(object sender, RoutedEventArgs e)
        {
            // Racing Brake: More sensitive at start, aggressive curve
            _curvePoints[0] = new Point(0.0, 0.0);
            _curvePoints[1] = new Point(0.1, 0.25);
            _curvePoints[2] = new Point(0.2, 0.40);
            _curvePoints[3] = new Point(0.3, 0.55);
            _curvePoints[4] = new Point(0.4, 0.68);
            _curvePoints[5] = new Point(0.5, 0.80);
            _curvePoints[6] = new Point(0.6, 0.88);
            _curvePoints[7] = new Point(0.7, 0.93);
            _curvePoints[8] = new Point(0.8, 0.96);
            _curvePoints[9] = new Point(0.9, 0.98);
            _curvePoints[10] = new Point(1.0, 1.0);
            DrawCurve();
            UpdateCurveDeviation();
        }

        private void OnPresetSmoothThrottle(object sender, RoutedEventArgs e)
        {
            // Smooth Throttle: Gentle curve, less sensitive at start
            _curvePoints[0] = new Point(0.0, 0.0);
            _curvePoints[1] = new Point(0.1, 0.05);
            _curvePoints[2] = new Point(0.2, 0.12);
            _curvePoints[3] = new Point(0.3, 0.22);
            _curvePoints[4] = new Point(0.4, 0.35);
            _curvePoints[5] = new Point(0.5, 0.50);
            _curvePoints[6] = new Point(0.6, 0.65);
            _curvePoints[7] = new Point(0.7, 0.78);
            _curvePoints[8] = new Point(0.8, 0.88);
            _curvePoints[9] = new Point(0.9, 0.95);
            _curvePoints[10] = new Point(1.0, 1.0);
            DrawCurve();
            UpdateCurveDeviation();
        }

        private void OnPresetPreciseSteering(object sender, RoutedEventArgs e)
        {
            // Precise Steering: S-curve, more sensitive in middle
            _curvePoints[0] = new Point(0.0, 0.0);
            _curvePoints[1] = new Point(0.1, 0.08);
            _curvePoints[2] = new Point(0.2, 0.18);
            _curvePoints[3] = new Point(0.3, 0.32);
            _curvePoints[4] = new Point(0.4, 0.42);
            _curvePoints[5] = new Point(0.5, 0.50);
            _curvePoints[6] = new Point(0.6, 0.58);
            _curvePoints[7] = new Point(0.7, 0.68);
            _curvePoints[8] = new Point(0.8, 0.82);
            _curvePoints[9] = new Point(0.9, 0.92);
            _curvePoints[10] = new Point(1.0, 1.0);
            DrawCurve();
            UpdateCurveDeviation();
        }

        private void OnPresetLinear(object sender, RoutedEventArgs e)
        {
            // Linear: Perfect 1:1 mapping
            for (int i = 0; i < NUM_POINTS; i++)
            {
                _curvePoints[i] = new Point(i / 10.0, i / 10.0);
            }
            DrawCurve();
            UpdateCurveDeviation();
        }

        private void OnResetCurve(object sender, RoutedEventArgs e)
        {
            OnPresetLinear(null, null);
        }

        // Profile Save/Load
        private void LoadAllProfiles()
        {
            try
            {
                LoadCurveCombo.Items.Clear();
                LoadCurveCombo.Items.Add("-- Save as new profile --");
                
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Values.ContainsKey(PROFILES_KEY))
                {
                    string profilesJson = localSettings.Values[PROFILES_KEY] as string;
                    if (!string.IsNullOrEmpty(profilesJson))
                    {
                        // Simple JSON parsing for profile names
                        var profiles = profilesJson.Split('|');
                        foreach (var profile in profiles.Where(p => !string.IsNullOrEmpty(p)))
                        {
                            var parts = profile.Split('=');
                            if (parts.Length == 2)
                            {
                                LoadCurveCombo.Items.Add(parts[0]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadAllProfiles error: {ex}");
            }
        }

        private async void OnSaveCurveProfile(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show input dialog for profile name
                var dialog = new ContentDialog
                {
                    Title = "Save Curve Profile",
                    Content = new TextBox { PlaceholderText = $"Profile name (e.g., {_currentAxisName} - Racing)" },
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var textBox = dialog.Content as TextBox;
                    string profileName = textBox.Text.Trim();
                    
                    if (!string.IsNullOrEmpty(profileName))
                    {
                        SaveProfile(profileName);
                        LoadAllProfiles();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnSaveCurveProfile error: {ex}");
            }
        }

        private void SaveProfile(string name)
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                string curveData = string.Join(",", _curvePoints.Select(p => p.Y.ToString("F4")));
                
                string allProfiles = localSettings.Values.ContainsKey(PROFILES_KEY) ? 
                    localSettings.Values[PROFILES_KEY] as string : "";
                
                // Remove existing profile with same name if exists
                if (allProfiles.Contains(name + "="))
                {
                    var profiles = allProfiles.Split('|');
                    allProfiles = string.Join("|", profiles.Where(p => !p.StartsWith(name + "=")));
                }
                
                // Add new profile
                if (!string.IsNullOrEmpty(allProfiles))
                    allProfiles += "|";
                allProfiles += $"{name}={curveData}";
                
                localSettings.Values[PROFILES_KEY] = allProfiles;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveProfile error: {ex}");
            }
        }

        private void OnLoadCurveProfile(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (LoadCurveCombo.SelectedIndex <= 0) return; // Skip the "Save as new" option
                
                string profileName = LoadCurveCombo.SelectedItem as string;
                if (string.IsNullOrEmpty(profileName)) return;
                
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Values.ContainsKey(PROFILES_KEY))
                {
                    string allProfiles = localSettings.Values[PROFILES_KEY] as string;
                    var profiles = allProfiles.Split('|');
                    
                    foreach (var profile in profiles)
                    {
                        var parts = profile.Split('=');
                        if (parts.Length == 2 && parts[0] == profileName)
                        {
                            var curveData = parts[1].Split(',');
                            for (int i = 0; i < NUM_POINTS && i < curveData.Length; i++)
                            {
                                if (double.TryParse(curveData[i], out double yValue))
                                {
                                    _curvePoints[i] = new Point(i / 10.0, yValue);
                                }
                            }
                            DrawCurve();
                            UpdateCurveDeviation();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnLoadCurveProfile error: {ex}");
            }
        }

        private void UpdateCurveDescription()
        {
            CurveDescription.Text = "10-point interactive curve - drag points to customize";
        }

        public void SetupAxisMappingCombo(DeviceInfo device, InputAxisType currentMapping, Action<InputAxisType> onMappingChanged)
        {
            try
            {
                _isUpdatingAxisSource = true;
                _mappingChangedCallback = onMappingChanged;
                AxisSourceCombo.Items.Clear();
                AxisSourceCombo.Items.Add(new ComboBoxItem
                {
                    Content = "None (Not Used)",
                    Tag = InputAxisType.Unknown
                });

                foreach (var axis in device.DetectedAxes)
                {
                    AxisSourceCombo.Items.Add(new ComboBoxItem
                    {
                        Content = axis.DisplayName,
                        Tag = axis.AxisType
                    });
                }

                for (var index = 0; index < AxisSourceCombo.Items.Count; index++)
                {
                    if (AxisSourceCombo.Items[index] is ComboBoxItem item &&
                        item.Tag is InputAxisType axisType &&
                        axisType == currentMapping)
                    {
                        AxisSourceCombo.SelectedIndex = index;
                        break;
                    }
                }

                _isUpdatingAxisSource = false;
            }
            catch (Exception ex)
            {
                _isUpdatingAxisSource = false;
                System.Diagnostics.Debug.WriteLine($"SetupAxisMappingCombo error: {ex}");
            }
        }

        private Action<InputAxisType>? _mappingChangedCallback;

        private void OnAxisSourceChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_isUpdatingAxisSource &&
                    sender is ComboBox comboBox &&
                    comboBox.SelectedItem is ComboBoxItem selectedItem &&
                    selectedItem.Tag is InputAxisType axisType)
                {
                    _mappingChangedCallback?.Invoke(axisType);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnAxisSourceChanged error: {ex}");
            }
        }
    }
}
