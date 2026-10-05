using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SimRacingPedalCalibrator.Models;
using System;
using System.Collections.Generic;

namespace SimRacingPedalCalibrator
{
    public sealed partial class AxisMappingDialog : ContentDialog
    {
        private DeviceInfo _device;
        private Dictionary<string, InputAxisType> _selectedAxes = new();
        private DispatcherTimer _updateTimer;

        public AxisMappingDialog(DeviceInfo device)
        {
            this.InitializeComponent();
            _device = device;
            
            System.Diagnostics.Debug.WriteLine($"[AxisMappingDialog] Opening for device: {device.DisplayName}");
            System.Diagnostics.Debug.WriteLine($"[AxisMappingDialog] Detected axes count: {device.DetectedAxes.Count}");
            foreach (var axis in device.DetectedAxes)
            {
                System.Diagnostics.Debug.WriteLine($"  - {axis.DisplayName} (Type: {axis.AxisType})");
            }
            
            LoadAxisOptions();
            LoadCurrentMapping();
            
            DeviceNameText.Text = _device.DisplayName;
            DetectedAxesText.Text = $"Detected Axes: {_device.DetectedAxes.Count}";

            // Start timer to update axis values
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(100);
            _updateTimer.Tick += UpdateAxisValues_Tick;
            _updateTimer.Start();

            this.Closed += (s, e) => _updateTimer?.Stop();
        }

        private void UpdateAxisValues_Tick(object? sender, object? e)
        {
            // Update throttle
            if (ThrottleAxisCombo.SelectedItem is (string _, InputAxisType throttleType))
            {
                UpdateAxisValue(ThrottleValueText, ThrottleBar, throttleType);
            }

            // Update brake
            if (BrakeAxisCombo.SelectedItem is (string _, InputAxisType brakeType))
            {
                UpdateAxisValue(BrakeValueText, BrakeBar, brakeType);
            }

            // Update clutch
            if (ClutchAxisCombo.SelectedItem is (string _, InputAxisType clutchType))
            {
                UpdateAxisValue(ClutchValueText, ClutchBar, clutchType);
            }
        }

        private void LoadAxisOptions()
        {
            System.Diagnostics.Debug.WriteLine($"[LoadAxisOptions] Adding axis options. Device has {_device.DetectedAxes.Count} axes");
            
            // Create list of available axes
            var axisOptions = new List<(string Display, InputAxisType Type)>
            {
                ("None (Not Used)", InputAxisType.Unknown)
            };

            // Add detected axes
            foreach (var axis in _device.DetectedAxes)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadAxisOptions] Adding axis: {axis.DisplayName} (Type: {axis.AxisType})");
                axisOptions.Add((axis.DisplayName, axis.AxisType));
            }
            
            // Bind ComboBoxes - clear existing items and use binding
            ThrottleAxisCombo.ItemsSource = axisOptions;
            BrakeAxisCombo.ItemsSource = axisOptions;
            ClutchAxisCombo.ItemsSource = axisOptions;

            // Set display/text properties
            ThrottleAxisCombo.DisplayMemberPath = "Display";
            BrakeAxisCombo.DisplayMemberPath = "Display";
            ClutchAxisCombo.DisplayMemberPath = "Display";
            
            System.Diagnostics.Debug.WriteLine($"[LoadAxisOptions] Combo boxes bound with {axisOptions.Count} items");
        }

        private void LoadCurrentMapping()
        {
            var mapping = _device.AxisMapping;

            // Set selections based on current mapping
            if (ThrottleAxisCombo.ItemsSource is List<(string Display, InputAxisType Type)> options)
            {
                var selectedIndex = options.FindIndex(o => o.Type == mapping.ThrottleAxis);
                if (selectedIndex >= 0)
                    ThrottleAxisCombo.SelectedIndex = selectedIndex;
                else
                    ThrottleAxisCombo.SelectedIndex = 0;

                selectedIndex = options.FindIndex(o => o.Type == mapping.BrakeAxis);
                if (selectedIndex >= 0)
                    BrakeAxisCombo.SelectedIndex = selectedIndex;
                else
                    BrakeAxisCombo.SelectedIndex = 0;

                selectedIndex = options.FindIndex(o => o.Type == mapping.ClutchAxis);
                if (selectedIndex >= 0)
                    ClutchAxisCombo.SelectedIndex = selectedIndex;
                else
                    ClutchAxisCombo.SelectedIndex = 0;
            }
        }

        private void OnThrottleAxisChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[OnThrottleAxisChanged] Selection changed");
            if (ThrottleAxisCombo.SelectedItem is (string Display, InputAxisType axisType))
            {
                System.Diagnostics.Debug.WriteLine($"[OnThrottleAxisChanged] Setting throttle axis to {axisType}");
                _device.AxisMapping.ThrottleAxis = axisType;
            }
        }

        private void OnBrakeAxisChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[OnBrakeAxisChanged] Selection changed");
            if (BrakeAxisCombo.SelectedItem is (string Display, InputAxisType axisType))
            {
                System.Diagnostics.Debug.WriteLine($"[OnBrakeAxisChanged] Setting brake axis to {axisType}");
                _device.AxisMapping.BrakeAxis = axisType;
            }
        }

        private void OnClutchAxisChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[OnClutchAxisChanged] Selection changed");
            if (ClutchAxisCombo.SelectedItem is (string Display, InputAxisType axisType))
            {
                System.Diagnostics.Debug.WriteLine($"[OnClutchAxisChanged] Setting clutch axis to {axisType}");
                _device.AxisMapping.ClutchAxis = axisType;
            }
        }

        private void UpdateAxisValue(TextBlock valueText, ProgressBar bar, InputAxisType axisType)
        {
            if (axisType == InputAxisType.Unknown)
            {
                valueText.Text = "Not assigned";
                bar.Value = 0;
                return;
            }

            var axis = _device.DetectedAxes.Find(a => a.AxisType == axisType);
            if (axis != null)
            {
                double percentage = (axis.CurrentValue / (double)(axis.MaxValue - axis.MinValue)) * 100;
                valueText.Text = $"Current Value: {axis.CurrentValue}/{axis.MaxValue}";
                bar.Value = Math.Clamp(percentage, 0, 100);
            }
            else
            {
                valueText.Text = "Axis not found";
                bar.Value = 0;
            }
        }

        public AxisMapping GetAxisMapping()
        {
            return _device.AxisMapping;
        }
    }
}

