using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SimRacingPedalCalibrator.Services;
using SimRacingPedalCalibrator.Models;
using Windows.UI.Core;
using Microsoft.UI.Windowing;

namespace SimRacingPedalCalibrator
{
    public sealed partial class MainWindow : Window
    {
        private readonly DeviceConnectionService _deviceService;
        private readonly RegistryService _registryService;
        private bool _isCalibrating = false;
        private CalibrationData _calibrationData = new();
        private ObservableCollection<DeviceInfo> _availableDevices = new();

        public MainWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBarGrid);

            _deviceService = new DeviceConnectionService();
            _registryService = new RegistryService();

            ((FrameworkElement)Content).Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow_Loaded started");
                
                // Set window size and center
                try
                {
                    this.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 700));
                    
                    var displayArea = DisplayArea.Primary;
                    var centeredX = displayArea.OuterBounds.X + (displayArea.OuterBounds.Width - 900) / 2;
                    var centeredY = displayArea.OuterBounds.Y + (displayArea.OuterBounds.Height - 700) / 2;
                    this.AppWindow.Move(new Windows.Graphics.PointInt32(centeredX, centeredY));
                    
                    System.Diagnostics.Debug.WriteLine("Window resized and centered");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Window sizing error: {ex.Message}");
                }
                
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded error: {ex}");
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("InitializeAsync: Starting device service initialization");
                // Initialize DirectInput
                if (_deviceService.Initialize())
                {
                    System.Diagnostics.Debug.WriteLine("InitializeAsync: Device service initialized successfully");
                    await RefreshAvailableDevicesAsync();
                }
                else
                {
                    DeviceStatusText.Text = "Failed to initialize device service";
                    System.Diagnostics.Debug.WriteLine("InitializeAsync: Device service failed to initialize");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeAsync error: {ex}");
                DeviceStatusText.Text = $"Error: {ex.Message}";
            }
        }

        private async Task RefreshAvailableDevicesAsync()
        {
            try
            {
                var devices = _deviceService.EnumerateDevices();
                _availableDevices.Clear();

                foreach (var device in devices)
                {
                    _availableDevices.Add(device);
                }

                DeviceComboBox.ItemsSource = _availableDevices;

                if (_availableDevices.Count > 0)
                {
                    DeviceComboBox.SelectedIndex = 0;
                    DeviceStatusText.Text = $"Found {_availableDevices.Count} device(s)";
                }
                else
                {
                    DeviceStatusText.Text = "No devices found";
                }
            }
            catch (Exception ex)
            {
                DeviceStatusText.Text = $"Error enumerating devices: {ex.Message}";
            }
        }

        private void OnRefreshDevicesClick(object sender, RoutedEventArgs e)
        {
            _ = RefreshAvailableDevicesAsync();
        }

        private async void OnConfigureAxisMappingClick(object sender, RoutedEventArgs e)
        {
            if (DeviceComboBox.SelectedItem is not DeviceInfo device)
            {
                var dialog = new ContentDialog
                {
                    Title = "No Device Selected",
                    Content = "Please select a device first.",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot
                };
                _ = await dialog.ShowAsync();
                return;
            }

            // Make sure device has detected axes
            if (device.DetectedAxes.Count == 0)
            {
                var dialog = new ContentDialog
                {
                    Title = "No Axes Detected",
                    Content = "This device has no detectable axes.",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot
                };
                _ = await dialog.ShowAsync();
                return;
            }

            // Show axis mapping dialog
            var mappingDialog = new AxisMappingDialog(device);
            mappingDialog.XamlRoot = Content.XamlRoot;
            
            var result = await mappingDialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                // Save the axis mapping
                AxisMappingRegistryService.SaveAxisMapping(device);
                
                var confirmDialog = new ContentDialog
                {
                    Title = "Axis Mapping Saved",
                    Content = "Axis configuration has been saved. Please restart the application to apply changes.",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot
                };
                _ = await confirmDialog.ShowAsync();
            }
        }

        private async void OnDeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceComboBox.SelectedItem is DeviceInfo device)
            {
                try
                {
                    // Load saved axis mapping
                    AxisMappingRegistryService.LoadAxisMapping(device);

                    if (_deviceService.ConnectToDevice(device))
                    {
                        // For serial ports, no registry lookup needed (yet)
                        if (device.ConnectionDeviceType == ConnectionDeviceType.DirectInput)
                        {
                            _calibrationData = _registryService.LoadCalibration(
                                _deviceService.VendorId,
                                _deviceService.ProductId);
                        }
                        else
                        {
                            // Use default calibration for serial devices
                            _calibrationData = new CalibrationData();
                        }

                        BrakeControl.SetCalibration(_calibrationData.Brake);
                        ThrottleControl.SetCalibration(_calibrationData.Throttle);
                        ClutchControl.SetCalibration(_calibrationData.Clutch);

                        DeviceStatusText.Text = $"Connected: {_deviceService.ConnectedDeviceName}";
                        SaveButton.IsEnabled = true;
                        
                        // Start polling for values
                        StartPollng();
                    }
                    else
                    {
                        DeviceStatusText.Text = $"Failed to connect to {device.ProductName}";
                        SaveButton.IsEnabled = false;
                    }
                }
                catch (Exception ex)
                {
                    DeviceStatusText.Text = $"Error: {ex.Message}";
                    SaveButton.IsEnabled = false;
                }
            }
        }

        private void StartPollng()
        {
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            timer.Tick += (s, e) =>
            {
                var values = _deviceService.GetAxisValues();
                if (values != null)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        BrakeControl.SetRawValue(values.Brake);
                        ThrottleControl.SetRawValue(values.Throttle);
                        ClutchControl.SetRawValue(values.Clutch);

                        if (_isCalibrating)
                        {
                            BrakeControl.UpdateCalibrationData(values.Brake);
                            ThrottleControl.UpdateCalibrationData(values.Throttle);
                            ClutchControl.UpdateCalibrationData(values.Clutch);
                        }
                    });
                }
            };
            timer.Start();
        }

        private void OnCalibrateClick(object sender, RoutedEventArgs e)
        {
            _isCalibrating = !_isCalibrating;
            
            if (_isCalibrating)
            {
                CalibrateButton.Content = "Stop Calibration";
                BrakeControl.ResetCalibration();
                ThrottleControl.ResetCalibration();
                ClutchControl.ResetCalibration();
            }
            else
            {
                CalibrateButton.Content = "Start Calibration";
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (_deviceService.ConnectedDeviceName is null)
                return;

            try
            {
                _calibrationData.Brake = BrakeControl.GetCalibration();
                _calibrationData.Throttle = ThrottleControl.GetCalibration();
                _calibrationData.Clutch = ClutchControl.GetCalibration();

                // Only save to registry for DirectInput devices
                if (DeviceComboBox.SelectedItem is DeviceInfo device && device.ConnectionDeviceType == ConnectionDeviceType.DirectInput)
                {
                    _registryService.SaveCalibration(
                        _calibrationData,
                        _deviceService.VendorId,
                        _deviceService.ProductId);
                }

                var dialog = new ContentDialog
                {
                    Title = "Success",
                    Content = "Calibration saved successfully!",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot
                };
                _ = dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to save calibration: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot
                };
                _ = dialog.ShowAsync();
            }
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            _calibrationData = new CalibrationData();
            BrakeControl.SetCalibration(_calibrationData.Brake);
            ThrottleControl.SetCalibration(_calibrationData.Throttle);
            ClutchControl.SetCalibration(_calibrationData.Clutch);
        }
    }
}

