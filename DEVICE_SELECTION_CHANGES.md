# Device Selection Feature - Implementation Guide

## Overview
The Sim Racing Pedal Calibrator application has been enhanced to support selecting from multiple COM ports and device types (chip sets) rather than being limited to a single hardcoded device.

## Changes Made

### 1. **Models** ([CalibrationModels.cs](Models/CalibrationModels.cs))
   - **New DeviceInfo Class**: Stores device information
     - `InstanceGuid`: Unique identifier for the device
     - `ProductName`: Name of the device (e.g., "EMCFFBV2")
     - `InstanceName`: Instance identifier
     - `VendorId` & `ProductId`: USB vendor and product IDs
     - `DisplayName`: User-friendly formatted name
     - `IsConnected`: Connection status flag

### 2. **Services - DirectInputService** ([Services/DirectInputService.cs](Services/DirectInputService.cs))
   
   **Key Enhancements:**
   
   - **EnumerateDevices()**: New method that scans for all connected GameControl devices
     - Returns a list of `DeviceInfo` objects
     - Supports all game controllers, not just EMCFFBV2
     - Extended device name matching to include "Joystick" and "Gamepad"
   
   - **ConnectToDevice(DeviceInfo device)**: New method for selective connection
     - Connects to a specific device by its InstanceGuid
     - Properly disposes previous connections
     - Initializes device axes and acquires device handle
     - Returns success/failure status
   
   - **Initialize()**: Simplified to just create DirectInput instance
   - All other methods remain unchanged for compatibility

### 3. **UI - MainWindow.xaml** ([MainWindow.xaml](MainWindow.xaml))
   
   **New UI Elements:**
   - Device selection ComboBox that displays all available devices
   - "Refresh" button to re-scan for devices
   - Improved status bar layout
   
   ```xaml
   <ComboBox x:Name="DeviceComboBox" Width="300" 
             SelectionChanged="OnDeviceSelectionChanged" 
             PlaceholderText="Available Devices..."/>
   <Button x:Name="RefreshDevicesButton" Content="Refresh" 
           Click="OnRefreshDevicesClick"/>
   ```

### 4. **Code-Behind - MainWindow.xaml.cs** ([MainWindow.xaml.cs](MainWindow.xaml.cs))
   
   **Major Changes:**
   
   - **ObservableCollection<DeviceInfo>**: Maintains list of available devices
   
   - **RefreshAvailableDevicesAsync()**: New method
     - Enumerates all connected devices
     - Populates ComboBox with devices
     - Automatically selects first device if available
   
   - **OnDeviceSelectionChanged()**: New event handler
     - Triggered when user selects a device from dropdown
     - Connects to selected device using new `ConnectToDevice()` method
     - Loads calibration data for the device
     - Starts polling for axis values
   
   - **OnRefreshDevicesClick()**: Allows manual device rescanning
   
   - **InitializeAsync()**: Updated to populate device list instead of auto-connecting

## Usage

### Selecting a Different Device
1. Click the "Refresh" button to scan for connected devices
2. Select a device from the dropdown menu
3. The application automatically:
   - Connects to the selected device
   - Loads its calibration data from the registry
   - Updates the status display
   - Begins polling for axis values

### Key Features
- ✅ Supports multiple device types (not just EMCFFBV2)
- ✅ Easy device switching without restarting
- ✅ Dynamic device enumeration
- ✅ Backward compatible with existing calibration registry data
- ✅ Each device's calibration stored separately by VendorId/ProductId

## Supported Device Types

The application now recognizes and supports devices with names containing:
- "EMCFFBV2" (original pedal box)
- "EMC"
- "PedalBox"
- "Joystick"
- "Gamepad"

To add support for additional device types, modify the `TargetNames` array in [DirectInputService.cs](Services/DirectInputService.cs).

## Technical Details

### Device Enumeration
- Uses SharpDX.DirectInput to enumerate GameControl devices
- Retrieves device capabilities and IDs
- No longer hardcoded to specific device names

### Connection Management
- Each device connection is properly disposed before establishing a new one
- Prevents device lock-ups from multiple simultaneous connections
- Gracefully handles disconnected devices

### Calibration Storage
- Registry path still uses VendorId/ProductId: `HKEY_CURRENT_USER\System\CurrentControlSet\Control\MediaProperties\PrivateProperties\DirectInput\VID_{vendorId:X4}&PID_{productId:X4}\Calibration\0\Type\Axes`
- Different devices maintain separate calibrations
- Existing calibration data is preserved

## Testing Recommendations

1. **Single Device**: Connect one pedal device and verify:
   - Device appears in dropdown on startup
   - Axis values update correctly
   - Calibration data loads and saves properly

2. **Multiple Devices**: Connect multiple USB gaming devices and verify:
   - All devices appear in dropdown
   - Can switch between devices
   - Each device maintains separate calibration

3. **Device Disconnection**: 
   - Disconnect device mid-operation
   - Verify graceful error handling
   - Reconnect and rescan

4. **Refresh Function**:
   - Add device while application running
   - Click Refresh button
   - Verify new device appears in list

## Troubleshooting

**No devices found?**
- Ensure device is connected and powered on
- Check Windows Device Manager to verify device detection
- Try clicking "Refresh" button

**Wrong device selected?**
- Look for device name in dropdown
- If unnamed, check Device Manager for USB VID/PID
- Ensure device is recognized as GameControl device

**Calibration not saving?**
- Verify device is still connected
- Check user has admin rights (registry access)
- Review error message in status bar

## Future Enhancements

Possible future improvements:
- Serial COM port support for direct serial communication
- Custom device name profiles
- Multi-axis custom mapping UI
- Device firmware information display
- Calibration presets by device type
