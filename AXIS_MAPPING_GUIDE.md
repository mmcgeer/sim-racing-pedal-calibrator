# Flexible Axis Mapping System - Implementation Guide

## Overview

The Sim Racing Pedal Calibrator now supports **flexible axis mapping**, allowing it to work with any device configuration—whether it's the standard X/Y/Z axes or custom configurations like your Arduino with Throttle/Brake/Z slider.

## Problem Solved

**Previous limitation:** The application assumed all devices had fixed X/Y/Z axes (Throttle/Brake/Clutch).

**Real-world issue:** Different devices expose different axes:
- Standard USB joystick: X, Y, Z, RX, RY, RZ
- Arduino serial device: Custom-named axes (Throttle, Brake, Z Slider)
- Fanatec CSL Elite: Rotational axes (RX, RY, RZ)
- Generic pedal controller: Slider1, Slider2, Z axis

**Solution:** Automatic axis detection + user-configurable mapping stored per device.

## New Features

### 1. Axis Detection
- DirectInput devices: Automatically detects all available axes
- Serial (COM port) devices: Assumes 3 axes (Throttle, Brake, Z slider) by default
- Falls back to standard X/Y/Z if detection fails

### 2. Axis Mapping Dialog
Located at: [Controls/AxisMappingDialog.xaml](Controls/AxisMappingDialog.xaml)

**Features:**
- Shows device name and number of detected axes
- Dropdown for each calibration axis (Throttle, Brake, Clutch)
- Real-time value display and progress bars
- "None (Not Used)" option for unused axes
- Live updates as you move pedals

**Usage:**
1. Select a device
2. Click "Configure Axes" button
3. Assign device axes to calibration axes using dropdowns
4. Move your pedals to verify values update correctly
5. Click "Save"

### 3. Persistent Storage
Registry path: `HKEY_CURRENT_USER\Software\SimRacingPedalCalibrator\AxisMappings`

**Stored per device:**
- DirectInput: `DirectInput_{GUID}`
- Serial: `SerialPort_{COM}`

**Stored data:**
- ThrottleAxis: Which device axis maps to Throttle
- BrakeAxis: Which device axis maps to Brake
- ClutchAxis: Which device axis maps to Clutch
- Device name and last saved timestamp

## Architecture

### New Models (CalibrationModels.cs)

```csharp
// Enum of all supported axis types
public enum InputAxisType
{
    X, Y, Z,              // Standard axes
    RX, RY, RZ,           // Rotational axes
    Slider1, Slider2,     // Slider/potentiometer axes
    Throttle, Brake,      // Named axes for serial devices
    Clutch, Unknown
}

// Represents a physical axis from a device
public class DeviceAxis
{
    public InputAxisType AxisType { get; set; }
    public string DisplayName { get; set; }
    public int Index { get; set; }              // For serial: byte position
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public int CurrentValue { get; set; }       // Real-time value
}

// Maps device axes to calibration axes
public class AxisMapping
{
    public string DeviceId { get; set; }
    public InputAxisType ThrottleAxis { get; set; }
    public InputAxisType BrakeAxis { get; set; }
    public InputAxisType ClutchAxis { get; set; }
}

// Extended DeviceInfo
public class DeviceInfo
{
    // ... existing properties ...
    public List<DeviceAxis> DetectedAxes { get; set; }
    public AxisMapping AxisMapping { get; set; }
}
```

### New Services

**AxisMappingRegistryService** (Services/AxisMappingRegistryService.cs)
- `SaveAxisMapping(DeviceInfo)` - Persist mapping to registry
- `LoadAxisMapping(DeviceInfo)` - Load saved mapping
- `DeleteAxisMapping(DeviceInfo)` - Remove old mappings

### Updated Services

**DirectInputService** (Services/DirectInputService.cs)
- `EnumerateDevices()` - Now detects and populates axes
- `ConnectToDevice()` - Unchanged
- `GetDirectInputAxisValues()` - Uses axis mapping instead of hardcoded positions
- `GetSerialAxisValues()` - Uses axis mapping for serial devices
- `DetectDeviceAxes()` - New: auto-detects device axes
- `MapDirectInputAxisToType()` - New: converts axis index to InputAxisType

### UI Components

**AxisMappingDialog** (Controls/AxisMappingDialog.xaml / .cs)
- ContentDialog-based modal for axis configuration
- 3 dropdowns for Throttle/Brake/Clutch assignments
- Real-time progress bars showing current axis values
- Timer-based updates (100ms interval) during configuration

**MainWindow** (MainWindow.xaml / .cs)
- New "Configure Axes" button next to Refresh button
- Auto-loads saved mappings on device selection
- Shows configuration dialog with error handling

## Data Flow

### Device Selection Flow
```
1. User selects device
   ↓
2. DirectInputService.EnumerateDevices() detects axes
   ↓
3. AxisMappingRegistryService.LoadAxisMapping() loads saved config
   ↓
4. DeviceConnectionService.ConnectToDevice()
   ↓
5. Polling loop uses AxisMapping to read correct axes
```

### Configuration Flow
```
1. User clicks "Configure Axes"
   ↓
2. AxisMappingDialog shows detected axes
   ↓
3. User selects mapping for each axis
   ↓
4. Real-time values update as user tests
   ↓
5. Click Save
   ↓
6. AxisMappingRegistryService.SaveAxisMapping()
   ↓
7. Registry persists configuration
```

### Polling Loop
```
// Old (hardcoded):
return new AxisValues
{
    Throttle = state.Z,
    Brake = state.X,
    Clutch = state.Y
};

// New (mapping-aware):
var axisValues = new Dictionary<InputAxisType, int>
{
    { InputAxisType.X, state.X },
    { InputAxisType.Y, state.Y },
    // ... etc ...
};

return new AxisValues
{
    Throttle = axisValues[mapping.ThrottleAxis],
    Brake = axisValues[mapping.BrakeAxis],
    Clutch = axisValues[mapping.ClutchAxis]
};
```

## Example: Arduino Setup

**Device:** Arduino with 3 analog inputs
- Input 0: Throttle pedal (0-1023 → 0-65535)
- Input 1: Brake pedal (0-1023 → 0-65535)
- Input 2: Z slider (0-1023 → 0-65535)

**Detected axes:**
- Throttle (Index 0)
- Brake (Index 1)
- Z Slider (Index 2)

**User configuration:**
- Throttle → Throttle ✓
- Brake → Brake ✓
- Clutch → Z Slider ✓

**Result:** Each axis is correctly calibrated to its physical input!

## Example: Fanatec CSL Elite Setup

**Device:** Fanatec CSL Elite wheel base
- Discovered axes: X, Y, Z, RX, RY, RZ

**User configuration:**
- Throttle → RZ (right pedal)
- Brake → RX (center pedal)
- Clutch → RY (left pedal)

**Stored mapping:** Registry saves this configuration per device

**Next connection:** Mapping auto-loads without re-configuration!

## Testing the Feature

### Test 1: Axis Detection
```
1. Launch app
2. Select a USB device
3. Click "Configure Axes"
4. Verify all axes are detected and named correctly
✓ Dialog shows correct axis count and names
```

### Test 2: Axis Value Display
```
1. In Configure Axes dialog
2. Move throttle pedal
3. Watch Throttle progress bar update in real-time
✓ Values change as you move pedals
✓ Progress bars reflect 0-100% of axis range
```

### Test 3: Configuration Save/Load
```
1. Configure axes (e.g., Throttle→X, Brake→Y, Clutch→Z)
2. Click Save
3. Close and reopen app
4. Select same device again
✓ Dialog shows same mapping as before
✓ No manual reconfiguration needed
```

### Test 4: Multiple Devices
```
1. Configure Device A (Throttle→X, Brake→Y, Clutch→Z)
2. Configure Device B (Throttle→Z, Brake→X, Clutch→Y)
3. Switch between devices
✓ Each device loads its own saved mapping
✓ Values correctly map despite different axes
```

## Backwards Compatibility

**Existing calibrations:** Preserved
- Old registry calibrations (VendorId/ProductId based) still load
- New axis mappings are stored separately
- Devices without saved mappings use auto-detected defaults

**Default mapping logic:**
- If 3+ axes detected: assigns first 3 to Throttle/Brake/Clutch
- If 2 axes: uses first 2 (Clutch unmapped)
- If 1 axis: uses it for Throttle (others unmapped)

## Future Enhancements

### Planned improvements:
1. **Axis name override** - Let users rename axes for clarity
2. **Axis range calibration** - Set min/max values per axis
3. **Axis filtering** - Hide unused axes from dropdown
4. **Quick presets** - "Fanatec Setup", "Arduino Setup", etc.
5. **Export/import** - Share configurations between users

### Potential additions:
- Support for more than 3 axes (multi-device profiles)
- Analog stick support (LX/LY/RX/RY)
- POV/hat switch support
- Button mapping for shift/clutch automation

## Technical Notes

### Axis Type Mapping (DirectInput → InputAxisType)
```
Index 0 → X
Index 1 → Y
Index 2 → Z
Index 3 → RX (Rotation X)
Index 4 → RY (Rotation Y)
Index 5 → RZ (Rotation Z)
Index 6+ → Slider1, Slider2
```

### Serial Port Assumptions
- Assumes 3 fixed axes in specific byte order
- Can be overridden via mapping dialog
- Future: Auto-detect from serial header

### Registry Location
- Path: `HKEY_CURRENT_USER\Software\SimRacingPedalCalibrator\AxisMappings`
- Per-device subkeys: `DirectInput_{GUID}` or `SerialPort_{COM}`
- Values: ThrottleAxis, BrakeAxis, ClutchAxis (enum strings)

## Known Limitations

1. **DirectInput axis detection** uses index-based mapping (not GUID-based)
   - Works for most devices
   - May vary on devices with custom axis ordering

2. **Serial port axes** are hardcoded to 3 axes
   - Future: configurable via firmware header

3. **No axis range auto-calibration**
   - User can manually set min/max values (future feature)

4. **Not applicable to buttons/triggers**
   - Only analog axes supported (future: add button support)

## Files Changed

### New Files
- `Controls/AxisMappingDialog.xaml` - UI for axis configuration
- `Controls/AxisMappingDialog.xaml.cs` - Dialog logic and real-time updates
- `Services/AxisMappingRegistryService.cs` - Registry persistence layer

### Modified Files
- `Models/CalibrationModels.cs` - Added InputAxisType, DeviceAxis, AxisMapping enums/classes
- `Services/DirectInputService.cs` - Axis detection and mapping-aware polling
- `MainWindow.xaml` - Added "Configure Axes" button
- `MainWindow.xaml.cs` - Added axis mapping dialog integration

## Conclusion

The flexible axis mapping system transforms the calibrator from a device-specific tool into a **universal pedal calibration solution**. Whether you're using a standard USB joystick, an Arduino controller, a Fanatec wheel base, or any other input device, the app automatically detects what axes are available and lets you map them to the calibration system—all with automatic persistence so your configuration is remembered.

This solves the original problem where devices with non-standard axis configurations (like your Arduino) couldn't be properly calibrated without hardcoding changes to the application.
