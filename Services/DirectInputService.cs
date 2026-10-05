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
                // Enumerate COM Ports
                var comPorts = SerialPort.GetPortNames().OrderBy(x => x).ToList();
                foreach (var portName in comPorts)
                {
                    devices.Add(new DeviceInfo
                    {
                        InstanceGuid = Guid.Empty,
                        ProductName = $"COM Port - {portName}",
                        InstanceName = portName,
                        VendorId = 0,
                        ProductId = 0,
                        IsConnected = false,
                        ConnectionDeviceType = ConnectionDeviceType.SerialPort,
                        ComPort = portName
                    });
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
                            
                            devices.Add(new DeviceInfo
                            {
                                InstanceGuid = device.InstanceGuid,
                                ProductName = device.ProductName,
                                InstanceName = device.InstanceName,
                                VendorId = joystick.Properties.VendorId,
                                ProductId = joystick.Properties.ProductId,
                                IsConnected = false,
                                ConnectionDeviceType = ConnectionDeviceType.DirectInput,
                                ComPort = null
                            });

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
                if (_joystick == null)
                    return null;

                _joystick.Poll();
                var state = _joystick.GetCurrentState();

                return new AxisValues
                {
                    Brake = state.RotationX,
                    Throttle = state.RotationZ,
                    Clutch = state.RotationY
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

                        _bufferIndex = 0;

                        return new AxisValues
                        {
                            Throttle = throttle,
                            Brake = brake,
                            Clutch = clutch
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
    }
}




