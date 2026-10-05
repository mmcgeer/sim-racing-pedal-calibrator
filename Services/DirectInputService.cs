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
        private byte[] _serialBuffer = new byte[6]; // 3 x 16-bit values
        private int _bufferIndex = 0;

        public int VendorId { get; private set; }
        public int ProductId { get; private set; }
        public string? ConnectedDeviceName { get; private set; }
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

                    // Add detected axes for serial port
                    device.DetectedAxes.Add(new DeviceAxis(InputAxisType.Throttle, "Throttle (Index 0)", 0));
                    device.DetectedAxes.Add(new DeviceAxis(InputAxisType.Brake, "Brake (Index 1)", 1));
                    device.DetectedAxes.Add(new DeviceAxis(InputAxisType.Slider1, "Z Slider (Index 2)", 2));

                    // Set default mapping for serial ports
                    device.AxisMapping = new AxisMapping(portName)
                    {
                        ThrottleAxis = InputAxisType.Throttle,
                        BrakeAxis = InputAxisType.Brake,
                        ClutchAxis = InputAxisType.Slider1
                    };

                    devices.Add(device);
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
                System.Diagnostics.Debug.WriteLine($"Error connecting to device: {ex.Message}");
                return false;
            }
        }

        private bool ConnectToSerialPort(DeviceInfo device)
        {
            try
            {
                _serialPort = new SerialPort(device.ComPort, 115200, Parity.None, 8, StopBits.One);
                _serialPort.ReadTimeout = 1000;
                _serialPort.WriteTimeout = 1000;
                _serialPort.Open();

                ConnectedDeviceName = device.ProductName;
                _currentDevice = device;
                device.IsConnected = true;
                _bufferIndex = 0;

                return true;
            }
            catch (Exception ex)
            {
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

                // Try to read available bytes
                while (_serialPort.BytesToRead > 0 && _bufferIndex < _serialBuffer.Length)
                {
                    _serialBuffer[_bufferIndex++] = (byte)_serialPort.ReadByte();

                    // When we have a complete packet
                    if (_bufferIndex == 6)
                    {
                        // Parse binary format: Throttle, Brake, Clutch (3 x 16-bit little-endian)
                        int throttle = BitConverter.ToUInt16(_serialBuffer, 0);
                        int brake = BitConverter.ToUInt16(_serialBuffer, 2);
                        int clutch = BitConverter.ToUInt16(_serialBuffer, 4);

                        // Update detected axes with current values
                        if (_currentDevice != null)
                        {
                            foreach (var axis in _currentDevice.DetectedAxes)
                            {
                                switch (axis.Index)
                                {
                                    case 0:
                                        axis.CurrentValue = throttle;
                                        break;
                                    case 1:
                                        axis.CurrentValue = brake;
                                        break;
                                    case 2:
                                        axis.CurrentValue = clutch;
                                        break;
                                }
                            }
                        }

                        _bufferIndex = 0;

                        // Get the axis mapping for current device
                        var mapping = _currentDevice?.AxisMapping;
                        if (mapping == null)
                        {
                            mapping = new AxisMapping();
                        }

                        return new AxisValues
                        {
                            Throttle = mapping.ThrottleAxis == InputAxisType.Throttle ? throttle : (mapping.ThrottleAxis == InputAxisType.Brake ? brake : clutch),
                            Brake = mapping.BrakeAxis == InputAxisType.Throttle ? throttle : (mapping.BrakeAxis == InputAxisType.Brake ? brake : clutch),
                            Clutch = mapping.ClutchAxis == InputAxisType.Throttle ? throttle : (mapping.ClutchAxis == InputAxisType.Brake ? brake : clutch)
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading serial data: {ex.Message}");
                return null;
            }
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




