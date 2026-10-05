# Axis Mapping Architecture & Technical Design

## System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                         User Interface                       │
├─────────────────────────────────────────────────────────────┤
│  MainWindow                    AxisMappingDialog            │
│  ┌──────────────────┐        ┌──────────────────┐          │
│  │ Select Device    │        │ Configure Axes   │          │
│  │ Refresh Devices  │        │ (Throttle/Brake) │          │
│  │ Configure Axes ──┼───────→│ Real-time Values │          │
│  │ Start/Stop Calib │        │ Progress Bars    │          │
│  └──────────────────┘        └──────────────────┘          │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│                  Business Logic Layer                       │
├─────────────────────────────────────────────────────────────┤
│  DeviceConnectionService      AxisMappingRegistry          │
│  ┌──────────────────────────┐ ┌─────────────────────────┐ │
│  │ EnumerateDevices()       │ │ SaveAxisMapping()       │ │
│  │ ConnectToDevice()        │ │ LoadAxisMapping()       │ │
│  │ GetAxisValues()          │ │ DeleteAxisMapping()     │ │
│  │ - GetDirectInputValues() │ │                         │ │
│  │ - GetSerialAxisValues()  │ └─────────────────────────┘ │
│  └──────────────────────────┘          ↓                  │
│                    ↓            Windows Registry           │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│                      Data Models                            │
├─────────────────────────────────────────────────────────────┤
│  DeviceInfo                InputAxisType                   │
│  ├─ InstanceGuid          ├─ X, Y, Z                      │
│  ├─ ProductName           ├─ RX, RY, RZ                   │
│  ├─ IsConnected           ├─ Slider1, Slider2             │
│  ├─ DetectedAxes[]        ├─ Throttle, Brake, Clutch      │
│  └─ AxisMapping           └─ Unknown                      │
│                                                             │
│  DeviceAxis              AxisMapping                      │
│  ├─ AxisType             ├─ ThrottleAxis                 │
│  ├─ DisplayName          ├─ BrakeAxis                    │
│  ├─ CurrentValue         └─ ClutchAxis                   │
│  └─ MaxValue                                              │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│                   Hardware Layer                            │
├─────────────────────────────────────────────────────────────┤
│  DirectInput (USB)              SerialPort (COM)            │
│  ├─ Joystick                   ├─ COM Port                 │
│  ├─ GetObjects(Axis)           ├─ Read bytes               │
│  ├─ Poll()                     └─ Parse 16-bit values      │
│  └─ GetCurrentState()                                      │
└─────────────────────────────────────────────────────────────┘
```

## Class Hierarchy

### Models/CalibrationModels.cs

```csharp
// Enum for all supported axis types
public enum InputAxisType
{
    X, Y, Z,              // Standard Cartesian axes
    RX, RY, RZ,           // Rotational axes
    Slider1, Slider2,     // Extra analog inputs
    Throttle, Brake,      // Named axes (for serial devices)
    Clutch,
    Unknown
}

// Represents a physical axis on the device
public class DeviceAxis
{
    public InputAxisType AxisType { get; set; }
    public string DisplayName { get; set; }      // "X Axis", "Throttle", etc.
    public int Index { get; set; }               // Position in array (for serial)
    public int MinValue { get; set; }            // Typically 0
    public int MaxValue { get; set; }            // Typically 65535
    public int CurrentValue { get; set; }        // Updated during polling
}

// Maps device's input axes to calibration axes
public class AxisMapping
{
    public string DeviceId { get; set; }         // Device identifier
    public InputAxisType ThrottleAxis { get; set; }
    public InputAxisType BrakeAxis { get; set; }
    public InputAxisType ClutchAxis { get; set; }
    
    public string? GetCalibrationAxisForInput(InputAxisType input)
    {
        // Returns "Throttle", "Brake", "Clutch", or null
    }
}

// Extended from previous version
public class DeviceInfo
{
    public Guid InstanceGuid { get; set; }
    public string ProductName { get; set; }
    public string? ComPort { get; set; }
    public ConnectionDeviceType ConnectionDeviceType { get; set; }
    
    // NEW: Detected axes and mapping
    public List<DeviceAxis> DetectedAxes { get; set; } = new();
    public AxisMapping AxisMapping { get; set; } = new();
}
```

## Service Layer

### DirectInputService.cs - Axis Detection

```csharp
public List<DeviceInfo> EnumerateDevices()
{
    // ... existing device enumeration ...
    
    // NEW: Detect available axes
    foreach (var device in devices)
    {
        if (device.ConnectionDeviceType == ConnectionDeviceType.DirectInput)
        {
            var joystick = new Joystick(_directInput, device.InstanceGuid);
            DetectDeviceAxes(joystick, device);  // NEW METHOD
            device.AxisMapping = new AxisMapping(device.InstanceGuid.ToString());
            joystick.Dispose();
        }
        else if (device.ConnectionDeviceType == ConnectionDeviceType.SerialPort)
        {
            // Serial ports: assume 3 named axes
            device.DetectedAxes.Add(new DeviceAxis(InputAxisType.Throttle, ...));
            device.DetectedAxes.Add(new DeviceAxis(InputAxisType.Brake, ...));
            device.DetectedAxes.Add(new DeviceAxis(InputAxisType.Slider1, ...));
            device.AxisMapping = new AxisMapping(device.ComPort);
        }
    }
    
    return devices;
}

private void DetectDeviceAxes(Joystick joystick, DeviceInfo deviceInfo)
{
    var axes = joystick.GetObjects(DeviceObjectTypeFlags.Axis);
    int axisIndex = 0;
    
    foreach (var axis in axes)
    {
        // Map DirectInput axis to our InputAxisType enum
        var inputAxisType = MapDirectInputAxisToType(axisIndex);
        
        deviceInfo.DetectedAxes.Add(new DeviceAxis(
            inputAxisType,
            $"{axis.Name} ({inputAxisType})",
            axisIndex
        )
        {
            MinValue = 0,
            MaxValue = 65535
        });
        
        axisIndex++;
    }
}

private InputAxisType MapDirectInputAxisToType(int index)
{
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
```

### DirectInputService.cs - Mapping-Aware Polling

```csharp
private AxisValues? GetDirectInputAxisValues()
{
    if (_joystick == null || _currentDevice == null)
        return null;
    
    _joystick.Poll();
    var state = _joystick.GetCurrentState();
    var mapping = _currentDevice.AxisMapping;
    
    // Create dictionary of detected axes to their current values
    var axisValues = new Dictionary<InputAxisType, int>
    {
        { InputAxisType.X, state.X },
        { InputAxisType.Y, state.Y },
        { InputAxisType.Z, state.Z },
        { InputAxisType.RX, state.RotationX },
        { InputAxisType.RY, state.RotationY },
        { InputAxisType.RZ, state.RotationZ },
        { InputAxisType.Slider1, state.Sliders.Length > 0 ? state.Sliders[0] : 0 },
        { InputAxisType.Slider2, state.Sliders.Length > 1 ? state.Sliders[1] : 0 }
    };
    
    // Update current values in detected axes (for UI display)
    foreach (var detectedAxis in _currentDevice.DetectedAxes)
    {
        if (axisValues.ContainsKey(detectedAxis.AxisType))
        {
            detectedAxis.CurrentValue = axisValues[detectedAxis.AxisType];
        }
    }
    
    // Return calibration values based on mapping
    return new AxisValues
    {
        Throttle = axisValues.ContainsKey(mapping.ThrottleAxis) 
            ? axisValues[mapping.ThrottleAxis] : 0,
        Brake = axisValues.ContainsKey(mapping.BrakeAxis) 
            ? axisValues[mapping.BrakeAxis] : 0,
        Clutch = axisValues.ContainsKey(mapping.ClutchAxis) 
            ? axisValues[mapping.ClutchAxis] : 0
    };
}

private AxisValues? GetSerialAxisValues()
{
    // Similar mapping-aware logic for serial ports
    // Reads 3 x 16-bit values, maps to Throttle/Brake/Clutch based on mapping
}
```

### AxisMappingRegistryService.cs - Persistence

```csharp
public class AxisMappingRegistryService
{
    private static readonly string REGISTRY_PATH = 
        @"Software\SimRacingPedalCalibrator\AxisMappings";
    
    public static void SaveAxisMapping(DeviceInfo device)
    {
        // Registry structure:
        // HKEY_CURRENT_USER\
        //   Software\
        //     SimRacingPedalCalibrator\
        //       AxisMappings\
        //         DirectInput_{GUID}\
        //           ThrottleAxis = "X"
        //           BrakeAxis = "Y"
        //           ClutchAxis = "Z"
        //           DeviceName = "Device Name"
        //           LastSaved = "2024-01-20T14:30:00"
    }
    
    public static void LoadAxisMapping(DeviceInfo device)
    {
        // Load saved mapping from registry
        // If not found, device retains auto-detected defaults
    }
    
    private static string GetDeviceId(DeviceInfo device)
    {
        return device.ConnectionDeviceType == ConnectionDeviceType.SerialPort
            ? $"SerialPort_{device.ComPort}"
            : $"DirectInput_{device.InstanceGuid}";
    }
}
```

## UI Layer

### AxisMappingDialog.xaml.cs

```csharp
public sealed partial class AxisMappingDialog : ContentDialog
{
    private DeviceInfo _device;
    private DispatcherTimer _updateTimer;
    
    public AxisMappingDialog(DeviceInfo device)
    {
        this.InitializeComponent();
        _device = device;
        
        // Populate dropdowns with detected axes
        LoadAxisOptions();      // Add all detected axes to dropdowns
        LoadCurrentMapping();   // Select currently mapped axes
        
        // Start timer for real-time value updates
        _updateTimer = new DispatcherTimer();
        _updateTimer.Interval = TimeSpan.FromMilliseconds(100);
        _updateTimer.Tick += UpdateAxisValues_Tick;
        _updateTimer.Start();
    }
    
    private void UpdateAxisValues_Tick(object? sender, object? e)
    {
        // Update progress bars with current axis values
        // Called every 100ms to show real-time pedal movement
    }
    
    private void OnThrottleAxisChanged(object sender, SelectionChangedEventArgs e)
    {
        // User selected a new axis for Throttle
        if (ThrottleAxisCombo.SelectedItem is ComboBoxItem item 
            && item.Tag is InputAxisType axisType)
        {
            _device.AxisMapping.ThrottleAxis = axisType;
            // Progress bar updates automatically in timer
        }
    }
}
```

### MainWindow.xaml.cs

```csharp
private async void OnConfigureAxisMappingClick(object sender, RoutedEventArgs e)
{
    if (DeviceComboBox.SelectedItem is not DeviceInfo device)
    {
        // Show error
        return;
    }
    
    // Show mapping dialog
    var mappingDialog = new AxisMappingDialog(device);
    var result = await mappingDialog.ShowAsync();
    
    if (result == ContentDialogResult.Primary)
    {
        // User clicked Save
        AxisMappingRegistryService.SaveAxisMapping(device);
        
        // Show confirmation
        // Note: user needs to restart app to use new mapping
    }
}

private async void OnDeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (DeviceComboBox.SelectedItem is DeviceInfo device)
    {
        // Load previously saved axis mapping
        AxisMappingRegistryService.LoadAxisMapping(device);
        
        // Connect to device
        _deviceService.ConnectToDevice(device);
        
        // Start polling with new mapping
        StartPolling();
    }
}
```

## Data Flow Sequences

### Device Selection → Polling

```
MainWindow
  └─ OnDeviceSelectionChanged
     └─ AxisMappingRegistryService.LoadAxisMapping(device)
        └─ registry lookup: DirectInput_{GUID}
           └─ device.AxisMapping = (loaded values)
     └─ DeviceConnectionService.ConnectToDevice(device)
        └─ DirectInputService.ConnectToDirectInputDevice(device)
           └─ _currentDevice = device (with mapping)
     └─ StartPolling()
        └─ Timer.Tick:
           └─ DeviceConnectionService.GetAxisValues()
              └─ DirectInputService.GetDirectInputAxisValues()
                 └─ Use device.AxisMapping to map raw axes → Throttle/Brake/Clutch
                    └─ Returns AxisValues(Throttle, Brake, Clutch)
              └─ Update UI with calibrated values
```

### Configure Axes Dialog

```
MainWindow
  └─ OnConfigureAxisMappingClick
     └─ new AxisMappingDialog(device)
        └─ DialogLoaded:
           └─ LoadAxisOptions()
              └─ Populate dropdowns with device.DetectedAxes
           └─ LoadCurrentMapping()
              └─ Select current device.AxisMapping values
           └─ Start _updateTimer (100ms)
              └─ UpdateAxisValues_Tick
                 └─ DirectInputService.Poll() gets latest axis values
                    └─ Update DetectedAxes[].CurrentValue
                       └─ UI progress bars update in real-time
     └─ User selects axes from dropdowns
        └─ device.AxisMapping updated in memory
     └─ User clicks Save
        └─ ContentDialogResult = Primary
           └─ AxisMappingRegistryService.SaveAxisMapping(device)
              └─ Registry persists device.AxisMapping
```

## Multi-Device Support

```
Device A (Fanatec Wheel Base)
├─ DetectedAxes: [X, Y, Z, RX, RY, RZ]
├─ AxisMapping:
│  ├─ ThrottleAxis = RZ
│  ├─ BrakeAxis = RX
│  └─ ClutchAxis = RY
└─ Registry Key: DirectInput_{GUID_A}

Device B (Arduino)
├─ DetectedAxes: [Throttle, Brake, ZSlider]
├─ AxisMapping:
│  ├─ ThrottleAxis = Throttle
│  ├─ BrakeAxis = Brake
│  └─ ClutchAxis = Slider1
└─ Registry Key: SerialPort_COM3

// When user switches devices:
1. Select Device A → LoadAxisMapping → Mapping A loaded
2. Select Device B → LoadAxisMapping → Mapping B loaded
3. Select Device A → LoadAxisMapping → Mapping A loaded (unchanged)
```

## Error Handling

```
Try/Catch in Key Areas:

1. DetectDeviceAxes()
   ├─ Fallback: If detection fails, use default X/Y/Z axes
   └─ Debug output of any exceptions

2. GetDirectInputAxisValues()
   ├─ Catch: Device disconnected mid-polling
   ├─ Return: null (UI handles gracefully)
   └─ Connection will retry on next poll

3. SaveAxisMapping() / LoadAxisMapping()
   ├─ Catch: Registry access denied
   ├─ Log: Debug message
   └─ Continue: Use in-memory mapping

4. AxisMappingDialog
   ├─ Catch: Device not selected
   ├─ Show: Error dialog to user
   └─ Require: Device selection before continuing
```

## Performance Considerations

```
Axis Detection:
- Once per EnumerateDevices() call (~startup, ~refresh click)
- ~10-50ms depending on device (minimal impact)

Registry I/O:
- Load: ~5ms per device (once at selection)
- Save: ~5ms per device (once when user saves mapping)

Real-time Polling:
- GetAxisValues() called ~60 times/sec
- Mapping lookup: O(1) dictionary key lookup
- ~<1ms per poll (same as before, negligible overhead)

UI Dialog Updates:
- AxisMappingDialog updates every 100ms
- Reads current values from _device.DetectedAxes[]
- Progress bar rendering handled by XAML framework
```

## Thread Safety

```
Polling Thread (UI Dispatcher):
- DirectInputService.Poll() → Updates device.AxisMapping
- MainWindow timer thread → Reads device.AxisMapping
- Safe: Both on UI dispatcher, no cross-thread access

Registry Access:
- AxisMappingRegistryService uses Registry class (thread-safe)
- No concurrent registry access expected

Device Detection:
- Runs on UI thread during device enumeration
- Safe: Single-threaded enumeration
```

## Future Extension Points

```
1. Custom Axis Names
   └─ Add device.DetectedAxes[].CustomName property
      └─ UI allows user to rename "X" → "Right Pedal"
         └─ Registry persists custom names

2. Multi-Axis Support (>3 axes)
   └─ Extend AxisMapping to support more axes
      └─ Add additional calibration axis types beyond Throttle/Brake/Clutch

3. Axis Range Calibration
   └─ Let user set min/max values per axis
      └─ Auto-calibrate on user button click

4. Import/Export Profiles
   └─ Export mappings to JSON
      └─ Share between users via file

5. Per-Game Profiles
   └─ Save different mappings per game
      └─ Auto-switch based on running process

6. Button Support
   └─ Extend beyond analog axes to digital buttons
      └─ Support shift/clutch automation via buttons
```

---

**Technical Design Document** | **Flexible Axis Mapping System** | **2024**
