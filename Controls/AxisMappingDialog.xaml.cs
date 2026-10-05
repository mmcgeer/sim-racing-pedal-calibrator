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
            if (ThrottleAxisCombo.SelectedItem is ComboBoxItem throttleItem && throttleItem.Tag is InputAxisType throttleType)
            {
                UpdateAxisValue(ThrottleValueText, ThrottleBar, throttleType);
            }

            // Update brake
            if (BrakeAxisCombo.SelectedItem is ComboBoxItem brakeItem && brakeItem.Tag is InputAxisType brakeType)
            {
                UpdateAxisValue(BrakeValueText, BrakeBar, brakeType);
            }

            // Update clutch
            if (ClutchAxisCombo.SelectedItem is ComboBoxItem clutchItem && clutchItem.Tag is InputAxisType clutchType)
            {
                UpdateAxisValue(ClutchValueText, ClutchBar, clutchType);
            }
        }

        private void LoadAxisOptions()
        {
            // Add "None" option
            var noneOption = new ComboBoxItem { Content = "None (Not Used)" };
            ThrottleAxisCombo.Items.Add(noneOption);
            BrakeAxisCombo.Items.Add(new ComboBoxItem { Content = "None (Not Used)" });
            ClutchAxisCombo.Items.Add(new ComboBoxItem { Content = "None (Not Used)" });

            // Add detected axes
            foreach (var axis in _device.DetectedAxes)
            {
                ThrottleAxisCombo.Items.Add(new ComboBoxItem 
                { 
                    Content = axis.DisplayName,
                    Tag = axis.AxisType
                });
                BrakeAxisCombo.Items.Add(new ComboBoxItem 
                { 
                    Content = axis.DisplayName,
                    Tag = axis.AxisType
                });
                ClutchAxisCombo.Items.Add(new ComboBoxItem 
                { 
                    Content = axis.DisplayName,
                    Tag = axis.AxisType
                });
            }
        }

        private void LoadCurrentMapping()
        {
            var mapping = _device.AxisMapping;

            // Set selections based on current mapping
            for (int i = 0; i < ThrottleAxisCombo.Items.Count; i++)
            {
                if (ThrottleAxisCombo.Items[i] is ComboBoxItem item && 
                    (item.Tag as InputAxisType?) == mapping.ThrottleAxis)
                {
                    ThrottleAxisCombo.SelectedIndex = i;
                    break;
                }
            }

            for (int i = 0; i < BrakeAxisCombo.Items.Count; i++)
            {
                if (BrakeAxisCombo.Items[i] is ComboBoxItem item && 
                    (item.Tag as InputAxisType?) == mapping.BrakeAxis)
                {
                    BrakeAxisCombo.SelectedIndex = i;
                    break;
                }
            }

            for (int i = 0; i < ClutchAxisCombo.Items.Count; i++)
            {
                if (ClutchAxisCombo.Items[i] is ComboBoxItem item && 
                    (item.Tag as InputAxisType?) == mapping.ClutchAxis)
                {
                    ClutchAxisCombo.SelectedIndex = i;
                    break;
                }
            }
        }

        private void OnThrottleAxisChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThrottleAxisCombo.SelectedItem is ComboBoxItem item && item.Tag is InputAxisType axisType)
            {
                _device.AxisMapping.ThrottleAxis = axisType;
            }
        }

        private void OnBrakeAxisChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BrakeAxisCombo.SelectedItem is ComboBoxItem item && item.Tag is InputAxisType axisType)
            {
                _device.AxisMapping.BrakeAxis = axisType;
            }
        }

        private void OnClutchAxisChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClutchAxisCombo.SelectedItem is ComboBoxItem item && item.Tag is InputAxisType axisType)
            {
                _device.AxisMapping.ClutchAxis = axisType;
            }
        }

        private void UpdateAxisValue(TextBlock valueText, ProgressBar bar, InputAxisType axisType)
        {
            var axis = _device.DetectedAxes.Find(a => a.AxisType == axisType);
            if (axis != null)
            {
                double percentage = (axis.CurrentValue / (double)(axis.MaxValue - axis.MinValue)) * 100;
                valueText.Text = $"Current Value: {axis.CurrentValue}/{axis.MaxValue}";
                bar.Value = Math.Clamp(percentage, 0, 100);
            }
        }

        public AxisMapping GetAxisMapping()
        {
            return _device.AxisMapping;
        }
    }
}

