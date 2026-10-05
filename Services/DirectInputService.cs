using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using SharpDX.DirectInput;
using SimRacingPedalCalibrator.Models;

namespace SimRacingPedalCalibrator.Services
{
    public class DeviceConnectionService
    {
        private DirectInput? _directInput;
        private Joystick? _joystick;
        private SerialPort? _serialPort;
        private DeviceInfo? _currentDevice;
        private string _serialLineBuffer = string.Empty;
        private int _throttleValue = 0;
        private int _brakeValue = 0;
        private int _clutchValue = 0;
        private int _throttleOutput = 0;
        private int _brakeOutput = 0;
        private int _clutchOutput = 0;
        private bool _hasSerialData;

        public int VendorId { get; private set; }
        public int ProductId { get; private set; }
        public string? ConnectedDeviceName { get; private set; }
        public string? LastConnectionError { get; private set; }
        public BoardProfile Board { get; set; } = BoardProfile.All[0];
        public string DetectedDevices { get; private set; } = "none";

        private static readonly string[] TargetNames = 
        { 
            "EMCFFBV2", "EMC", "PedalBox",
            "Joystick", "Gamepad"
        };

        public bool Initialize()
        {
            try
            {
                _directInput = new DirectInput();
                return true;
            }
            catch (Exception ex)
            {
                DetectedDevices = "error: " + ex.Message;
                return false;
            }
        }

        public List<DeviceInfo> EnumerateDevices()
        {
            var devices = new List<DeviceInfo>();
            try
            {
                // Enumerate COM Ports (assume 3 axes for serial: Throttle, Brake, Clutch)
                var comPorts = SerialPort.GetPortNames().OrderBy(x => x).ToList();
                System.Diagnostics.Debug.WriteLine($"[EnumerateDevices] Found {comPorts.Count} COM ports: {string.Join(", ", comPorts)}");

                foreach (var portName in comPorts)
                {
                    var device = new DeviceInfo
                    {
                        InstanceGuid = Guid.Empty,
                        ProductName = $"COM Port - {portName}",
                        InstanceName = portName,
                        VendorId = 0,
                        ProductId = 0,
                        IsConnected = false,
                        ConnectionDeviceType = ConnectionDeviceType.SerialPort,
                        ComPort = portName
                    };

                    var adcMax = Board.AdcMaxValue;
                    var throttleAxis = new DeviceAxis(InputAxisType.Throttle, "Throttle (A0)", 0) { MaxValue = adcMax };
                    var brakeAxis = new DeviceAxis(InputAxisType.Brake, "Brake (A2)", 1) { MaxValue = adcMax };
                    var clutchAxis = new DeviceAxis(InputAxisType.Slider1, "Clutch (A1)", 2) { MaxValue = adcMax };

                    device.DetectedAxes.Add(throttleAxis);
                    device.DetectedAxes.Add(brakeAxis);
                    device.DetectedAxes.Add(clutchAxis);

                    // Set default mapping for serial ports
                    device.AxisMapping = new AxisMapping(portName)
                    {
                        ThrottleAxis = InputAxisType.Throttle,
                        BrakeAxis = InputAxisType.Brake,
                        ClutchAxis = InputAxisType.Slider1
                    };

                    devices.Add(device);
                    System.Diagnostics.Debug.WriteLine($"[EnumerateDevices] Added COM port device: {device.DisplayName}");
                }

                // Enumerate USB GameControl Devices
                if (_directInput != null)
                {
                    var gameControllers = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);

                    foreach (var device in gameControllers)
                    {
                        try
                        {
                            var joystick = new Joystick(_directInput, device.InstanceGuid);
                            var deviceInfo = new DeviceInfo
                            {
                                InstanceGuid = device.InstanceGuid,
                                ProductName = device.ProductName,
                                InstanceName = device.InstanceName,
                                VendorId = joystick.Properties.VendorId,
                                ProductId = joystick.Properties.ProductId,
                                IsConnected = false,
                                ConnectionDeviceType = ConnectionDeviceType.DirectInput,
                                ComPort = null
                            };

                            // Detect available axes on the device
                            DetectDeviceAxes(joystick, deviceInfo);

                            // Set default mapping
                            deviceInfo.AxisMapping = new AxisMapping(device.InstanceGuid.ToString());

                            devices.Add(deviceInfo);
                            joystick.Dispose();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error enumerating device: {ex.Message}");
                        }
                    }
                }

                DetectedDevices = devices.Count == 0 ? "none" : string.Join("; ", 
                    devices.ConvertAll(d => $"{d.ProductName}/{d.InstanceName}"));
            }
            catch (Exception ex)
            {
                DetectedDevices = "error: " + ex.Message;
            }

            return devices;
        }

        public bool ConnectToDevice(DeviceInfo device)
        {
            LastConnectionError = null;
            try
            {
                // Disconnect previous device
                DisconnectCurrent();

                if (device.ConnectionDeviceType == ConnectionDeviceType.SerialPort)
                {
                    return ConnectToSerialPort(device);
                }
                else
                {
                    return ConnectToDirectInputDevice(device);
                }
            }
            catch (Exception ex)
            {
                LastConnectionError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"Error connecting to device: {ex.Message}");
                return false;
            }
        }

        private bool ConnectToSerialPort(DeviceInfo device)
        {
            try
            {
                _serialPort = new SerialPort(device.ComPort, Board.BaudRate, Parity.None, 8, StopBits.One);
                _serialPort.ReadTimeout = 1000;
                _serialPort.WriteTimeout = 1000;
                // Native-USB boards (Pro Micro/Leonardo) only stream when DTR is asserted
                _serialPort.DtrEnable = true;
                _serialPort.RtsEnable = true;
                _serialPort.Open();

                ConnectedDeviceName = device.ProductName;
                _currentDevice = device;
                device.IsConnected = true;
                _serialLineBuffer = string.Empty;
                _throttleValue = 0;
                _brakeValue = 0;
                _clutchValue = 0;
                _throttleOutput = 0;
                _brakeOutput = 0;
                _clutchOutput = 0;
                _hasSerialData = false;

                return true;
            }
            catch (Exception ex)
            {
                LastConnectionError = $"Unable to open {device.ComPort}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error opening serial port: {ex.Message}");
                _serialPort?.Dispose();
                _serialPort = null;
                return false;
            }
        }

        private bool ConnectToDirectInputDevice(DeviceInfo device)
        {
            try
            {
                if (_directInput == null)
                {
                    _directInput = new DirectInput();
                }

                var joystick = new Joystick(_directInput, device.InstanceGuid);
                VendorId = joystick.Properties.VendorId;
                ProductId = joystick.Properties.ProductId;
                ConnectedDeviceName = device.ProductName;
                
                joystick.Properties.BufferSize = 128;
                
                foreach (var axis in joystick.GetObjects(DeviceObjectTypeFlags.Axis))
                {
                    joystick.GetObjectPropertiesById(axis.ObjectId).Range = new InputRange(0, 65535);
                }

                joystick.Acquire();
                _joystick = joystick;
                _currentDevice = device;
                device.IsConnected = true;

                return true;
            }
            catch (Exception ex)
            {
                LastConnectionError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"Error connecting to DirectInput device: {ex.Message}");
                return false;
            }
        }

        private void DisconnectCurrent()
        {
            if (_joystick != null)
            {
                try
                {
                    _joystick.Unacquire();
                    _joystick.Dispose();
                }
                catch { }
                _joystick = null;
            }

            if (_serialPort != null)
            {
                try
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                }
                catch { }
                _serialPort = null;
            }

            if (_currentDevice != null)
            {
                _currentDevice.IsConnected = false;
                _currentDevice = null;
            }
        }

        public AxisValues? GetAxisValues()
        {
            try
            {
                if (_currentDevice?.ConnectionDeviceType == ConnectionDeviceType.SerialPort)
                {
                    return GetSerialAxisValues();
                }
                else if (_joystick != null)
                {
                    return GetDirectInputAxisValues();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading axis values: {ex.Message}");
            }

            return null;
        }

        private AxisValues? GetDirectInputAxisValues()
        {
            try
            {
                if (_joystick == null || _currentDevice == null)
                    return null;

                _joystick.Poll();
                var state = _joystick.GetCurrentState();

                // Get the axis mapping for current device
                var mapping = _currentDevice.AxisMapping;

                // Create a dictionary of axis types to values from the joystick state
                var sliders = state.Sliders;
                var axisValues = new Dictionary<InputAxisType, int>
                {
                    { InputAxisType.X, state.X },
                    { InputAxisType.Y, state.Y },
                    { InputAxisType.Z, state.Z },
                    { InputAxisType.RX, state.RotationX },
                    { InputAxisType.RY, state.RotationY },
                    { InputAxisType.RZ, state.RotationZ },
                    { InputAxisType.Slider1, sliders.Length > 0 ? sliders[0] : 0 },
                    { InputAxisType.Slider2, sliders.Length > 1 ? sliders[1] : 0 }
                };

                // Update the detected axes with current values
                foreach (var detectedAxis in _currentDevice.DetectedAxes)
                {
                    if (axisValues.ContainsKey(detectedAxis.AxisType))
                    {
                        detectedAxis.CurrentValue = axisValues[detectedAxis.AxisType];
                    }
                }

                // Get values based on the device's axis mapping
                return new AxisValues
                {
                    Throttle = axisValues.ContainsKey(mapping.ThrottleAxis) ? axisValues[mapping.ThrottleAxis] : 0,
                    Brake = axisValues.ContainsKey(mapping.BrakeAxis) ? axisValues[mapping.BrakeAxis] : 0,
                    Clutch = axisValues.ContainsKey(mapping.ClutchAxis) ? axisValues[mapping.ClutchAxis] : 0
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading DirectInput state: {ex.Message}");
                return null;
            }
        }

        private AxisValues? GetSerialAxisValues()
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                    return null;

                // Read all available characters into the buffer
                while (_serialPort.BytesToRead > 0)
                {
                    char ch = (char)_serialPort.ReadByte();

                    if (ch == '\n')
                    {
                        // End of line - parse the complete string
                        if (!string.IsNullOrEmpty(_serialLineBuffer))
                        {
                            _hasSerialData |= ParsePedalFXString(_serialLineBuffer);
                            _serialLineBuffer = string.Empty;
                        }
                    }
                    else if (ch != '\r')
                    {
                        _serialLineBuffer += ch;
                        if (_serialLineBuffer.Length > 1024)
                            _serialLineBuffer = string.Empty;
                    }
                }

                if (!_hasSerialData)
                    return null;

                // Create a dictionary of available axis types to serial values
                var axisValues = new Dictionary<InputAxisType, int>
                {
                    { InputAxisType.Throttle, _throttleValue },
                    { InputAxisType.Brake, _brakeValue },
                    { InputAxisType.Slider1, _clutchValue }
                };

                // Update detected axes with current values
                if (_currentDevice != null)
                {
                    foreach (var axis in _currentDevice.DetectedAxes)
                    {
                        if (axisValues.ContainsKey(axis.AxisType))
                        {
                            axis.CurrentValue = axisValues[axis.AxisType];
                        }
                    }
                }

                // Get the axis mapping for current device
                var mapping = _currentDevice?.AxisMapping;
                if (mapping == null)
                {
                    mapping = new AxisMapping();
                }

                var outputValues = new Dictionary<InputAxisType, int>
                {
                    { InputAxisType.Throttle, _throttleOutput },
                    { InputAxisType.Brake, _brakeOutput },
                    { InputAxisType.Slider1, _clutchOutput }
                };

                // Get values based on the device's axis mapping
                return new AxisValues
                {
                    Throttle = axisValues.ContainsKey(mapping.ThrottleAxis) ? axisValues[mapping.ThrottleAxis] : 0,
                    Brake = axisValues.ContainsKey(mapping.BrakeAxis) ? axisValues[mapping.BrakeAxis] : 0,
                    Clutch = axisValues.ContainsKey(mapping.ClutchAxis) ? axisValues[mapping.ClutchAxis] : 0,
                    ThrottleOutput = outputValues.TryGetValue(mapping.ThrottleAxis, out var t) ? t : null,
                    BrakeOutput = outputValues.TryGetValue(mapping.BrakeAxis, out var b) ? b : null,
                    ClutchOutput = outputValues.TryGetValue(mapping.ClutchAxis, out var c) ? c : null
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading serial data: {ex.Message}");
                return null;
            }
        }

        private bool ParsePedalFXString(string line)
        {
            var parsedAnyPedal = false;
            var pedals = line.Split(',');
            foreach (var pedal in pedals)
            {
                if (string.IsNullOrWhiteSpace(pedal))
                    continue;

                string trimmedPedal = pedal.Trim();

                // Format: T:0;0;671;0
                // Split by ':' first to separate type from values
                var typeSplit = trimmedPedal.Split(':');
                if (typeSplit.Length < 2)
                    continue;

                string pedalType = typeSplit[0].Trim();
                var values = typeSplit[1].Split(';');
                if (values.Length < 4)
                    continue;

                // The third field is the raw ADC value; the fourth is PedalFX's own calibrated (HID) output.
                if (!int.TryParse(values[2], out int value))
                    continue;
                int.TryParse(values[3], out int firmwareOutput);

                value = Math.Min(Board.AdcMaxValue, Math.Max(0, value));
                firmwareOutput = Math.Max(0, firmwareOutput);

                switch (pedalType)
                {
                    case "T":
                        _throttleValue = value;
                        _throttleOutput = firmwareOutput;
                        parsedAnyPedal = true;
                        break;
                    case "B":
                        _brakeValue = value;
                        _brakeOutput = firmwareOutput;
                        parsedAnyPedal = true;
                        break;
                    case "C":
                        _clutchValue = value;
                        _clutchOutput = firmwareOutput;
                        parsedAnyPedal = true;
                        break;
                }
            }

            return parsedAnyPedal;
        }

        public void Dispose()
        {
            DisconnectCurrent();
            _directInput?.Dispose();
        }

        private void DetectDeviceAxes(Joystick joystick, DeviceInfo deviceInfo)
        {
            try
            {
                var axes = joystick.GetObjects(DeviceObjectTypeFlags.Axis);
                int axisIndex = 0;

                foreach (var axis in axes)
                {
                    // Map DirectInput axis objects to our InputAxisType enum
                    var inputAxisType = MapDirectInputAxisToType(axisIndex);
                    var displayName = $"{axis.Name} ({inputAxisType})";

                    deviceInfo.DetectedAxes.Add(new DeviceAxis(
                        inputAxisType,
                        displayName,
                        axisIndex
                    )
                    {
                        MinValue = 0,
                        MaxValue = 65535
                    });

                    axisIndex++;
                }

                // If no axes detected, provide defaults
                if (deviceInfo.DetectedAxes.Count == 0)
                {
                    deviceInfo.DetectedAxes.Add(new DeviceAxis(InputAxisType.X, "X Axis", 0));
                    deviceInfo.DetectedAxes.Add(new DeviceAxis(InputAxisType.Y, "Y Axis", 1));
                    deviceInfo.DetectedAxes.Add(new DeviceAxis(InputAxisType.Z, "Z Axis", 2));
                }

                // Set reasonable defaults based on detected axes
                if (deviceInfo.DetectedAxes.Count >= 1)
                    deviceInfo.AxisMapping.ThrottleAxis = deviceInfo.DetectedAxes[0].AxisType;
                if (deviceInfo.DetectedAxes.Count >= 2)
                    deviceInfo.AxisMapping.BrakeAxis = deviceInfo.DetectedAxes[1].AxisType;
                if (deviceInfo.DetectedAxes.Count >= 3)
                    deviceInfo.AxisMapping.ClutchAxis = deviceInfo.DetectedAxes[2].AxisType;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error detecting axes: {ex.Message}");
            }
        }

        private InputAxisType MapDirectInputAxisToType(int index)
        {
            // Map DirectInput axis index to our InputAxisType enum
            // This is a simple index-based mapping for common device configurations
            return index switch
            {
                0 => InputAxisType.X,
                1 => InputAxisType.Y,
                2 => InputAxisType.Z,
                3 => InputAxisType.RX,
                4 => InputAxisType.RY,
                5 => InputAxisType.RZ,
                6 => InputAxisType.Slider1,
                7 => InputAxisType.Slider2,
                _ => InputAxisType.Unknown
            };
        }
    }
}
