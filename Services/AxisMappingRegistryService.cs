using Microsoft.Win32;
using SimRacingPedalCalibrator.Models;
using System;

namespace SimRacingPedalCalibrator.Services
{
    public class AxisMappingRegistryService
    {
        private static readonly string REGISTRY_PATH = @"Software\SimRacingPedalCalibrator\AxisMappings";

        public static void SaveAxisMapping(DeviceInfo device)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_PATH))
                {
                    if (key == null) return;

                    // Use device ID as subkey
                    string deviceId = GetDeviceId(device);
                    using (var deviceKey = key.CreateSubKey(deviceId))
                    {
                        if (deviceKey == null) return;

                        var mapping = device.AxisMapping;
                        deviceKey.SetValue("ThrottleAxis", mapping.ThrottleAxis.ToString());
                        deviceKey.SetValue("BrakeAxis", mapping.BrakeAxis.ToString());
                        deviceKey.SetValue("ClutchAxis", mapping.ClutchAxis.ToString());
                        deviceKey.SetValue("DeviceName", device.DisplayName);
                        deviceKey.SetValue("LastSaved", DateTime.Now.ToString("o"));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving axis mapping: {ex.Message}");
            }
        }

        public static void LoadAxisMapping(DeviceInfo device)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH))
                {
                    if (key == null) return;

                    string deviceId = GetDeviceId(device);
                    using (var deviceKey = key.OpenSubKey(deviceId))
                    {
                        if (deviceKey == null) return;

                        var throttleStr = deviceKey.GetValue("ThrottleAxis") as string;
                        var brakeStr = deviceKey.GetValue("BrakeAxis") as string;
                        var clutchStr = deviceKey.GetValue("ClutchAxis") as string;

                        if (Enum.TryParse<InputAxisType>(throttleStr, out var throttle))
                            device.AxisMapping.ThrottleAxis = throttle;

                        if (Enum.TryParse<InputAxisType>(brakeStr, out var brake))
                            device.AxisMapping.BrakeAxis = brake;

                        if (Enum.TryParse<InputAxisType>(clutchStr, out var clutch))
                            device.AxisMapping.ClutchAxis = clutch;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading axis mapping: {ex.Message}");
            }
        }

        private static string GetDeviceId(DeviceInfo device)
        {
            // Use different identifiers for different device types
            if (device.ConnectionDeviceType == ConnectionDeviceType.SerialPort)
                return $"SerialPort_{device.ComPort}";
            else
                return $"DirectInput_{device.InstanceGuid}";
        }

        public static void DeleteAxisMapping(DeviceInfo device)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH, true))
                {
                    if (key == null) return;

                    string deviceId = GetDeviceId(device);
                    key.DeleteSubKey(deviceId, false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting axis mapping: {ex.Message}");
            }
        }
    }
}
