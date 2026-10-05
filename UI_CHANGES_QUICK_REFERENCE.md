# UI Changes - Quick Reference

## Main Window Layout Update

### BEFORE (Original)
```
┌─────────────────────────────────────────────────────┐
│ Sim Racing Pedal Calibrator                         │
├─────────────────────────────────────────────────────┤
│ Device Status: Searching...                         │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Brake Calibration Panel                           │
│  ┌─────────────────────────────────────────────┐   │
│  │ Min: [____] Max: [____] DeadZone: [____]    │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  Throttle Calibration Panel                        │
│  ┌─────────────────────────────────────────────┐   │
│  │ Min: [____] Max: [____] DeadZone: [____]    │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  Clutch Calibration Panel                          │
│  ┌─────────────────────────────────────────────┐   │
│  │ Min: [____] Max: [____] DeadZone: [____]    │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│              [Start Calibration] [Save] [Reset]    │
└─────────────────────────────────────────────────────┘
```

### AFTER (Updated with Device Selection)
```
┌──────────────────────────────────────────────────────────────────┐
│ Sim Racing Pedal Calibrator                                      │
├──────────────────────────────────────────────────────────────────┤
│ Device Status: Connected: EMCFFBV2                               │
│                                                                  │
│ Select Device: ┌────────────────────────────┐  [Refresh]        │
│                │ EMCFFBV2 (Joystick - EMC) │                    │
│                │ Thrustmaster (Wheel)      │                    │
│                │ Generic Joystick          │                    │
│                └────────────────────────────┘                    │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Brake Calibration Panel                                        │
│  ┌────────────────────────────────────────────────────┐          │
│  │ Min: [____] Max: [____] DeadZone: [____]           │          │
│  └────────────────────────────────────────────────────┘          │
│                                                                  │
│  Throttle Calibration Panel                                     │
│  ┌────────────────────────────────────────────────────┐          │
│  │ Min: [____] Max: [____] DeadZone: [____]           │          │
│  └────────────────────────────────────────────────────┘          │
│                                                                  │
│  Clutch Calibration Panel                                       │
│  ┌────────────────────────────────────────────────────┐          │
│  │ Min: [____] Max: [____] DeadZone: [____]           │          │
│  └────────────────────────────────────────────────────┘          │
│                                                                  │
│           [Start Calibration] [Save] [Reset]                    │
└──────────────────────────────────────────────────────────────────┘
```

## New UI Elements

### 1. Device Selection ComboBox
**Location:** Top-right corner of status bar

```xaml
<ComboBox 
    x:Name="DeviceComboBox"
    Width="300"
    SelectionChanged="OnDeviceSelectionChanged"
    PlaceholderText="Available Devices..."/>
```

**Behavior:**
- Shows list of all connected GameControl devices
- Selecting a device automatically connects to it
- Loads that device's calibration data
- Updates status text

**Display Format:**
```
ProductName (InstanceName)
Example: EMCFFBV2 (Joystick - EMCFFBV2 Pedals)
```

### 2. Refresh Devices Button
**Location:** Right of device dropdown

```xaml
<Button 
    x:Name="RefreshDevicesButton"
    Content="Refresh" 
    Click="OnRefreshDevicesClick"/>
```

**Behavior:**
- Rescans for connected devices
- Updates the dropdown list
- Useful when:
  - Device is just plugged in
  - Device was unplugged
  - Manual refresh needed

## Code Changes by File

### MainWindow.xaml
**Changes:**
- Updated status bar from simple layout to two-column layout
- Added device dropdown ComboBox
- Added refresh button
- Grid with ColumnDefinitions for layout control

### MainWindow.xaml.cs
**New Members:**
```csharp
private ObservableCollection<DeviceInfo> _availableDevices;
```

**New Methods:**
- `RefreshAvailableDevicesAsync()` - Scan for devices
- `OnRefreshDevicesClick()` - Handle refresh button
- `OnDeviceSelectionChanged()` - Handle device selection

**Modified Methods:**
- `InitializeAsync()` - Now calls RefreshAvailableDevicesAsync
- `StartPollng()` - Unchanged

### Services/DirectInputService.cs
**New Methods:**
```csharp
public List<DeviceInfo> EnumerateDevices()
public bool ConnectToDevice(DeviceInfo device)
```

**Modified Methods:**
- `Initialize()` - Simplified, just creates DirectInput instance

### Models/CalibrationModels.cs
**New Class:**
```csharp
public class DeviceInfo
{
    public Guid InstanceGuid { get; set; }
    public string ProductName { get; set; }
    public string InstanceName { get; set; }
    public int VendorId { get; set; }
    public int ProductId { get; set; }
    public string DisplayName => $"{ProductName} ({InstanceName})";
    public bool IsConnected { get; set; }
}
```

## User Interaction Flow

```
User Starts App
    ↓
[MainWindow_Loaded]
    ↓
[Initialize()] → [DirectInputService.Initialize()]
    ↓
[RefreshAvailableDevicesAsync()]
    ↓
[DirectInputService.EnumerateDevices()] returns List<DeviceInfo>
    ↓
ComboBox populated with devices
    ↓
User sees: "Select Device:" [dropdown ▼] [Refresh]
    ↓
User selects device or clicks Refresh
    ↓
[OnDeviceSelectionChanged()] or [OnRefreshDevicesClick()]
    ↓
[DirectInputService.ConnectToDevice(device)]
    ↓
Device connected, status updated
    ↓
Calibration data loaded
    ↓
Polling starts - axis values displayed
```

## Error Handling Display

All errors are shown in the Device Status text:

| Status Message | Meaning | Action |
|---|---|---|
| `Searching...` | Initial state, scanning devices | Wait for completion |
| `Found X device(s)` | Devices detected | Select from dropdown |
| `No devices found` | No GameControl devices connected | Check Device Manager |
| `Connected: [DeviceName]` | Device is connected and ready | Proceed with calibration |
| `Failed to connect to...` | Connection failed | Check device, try refresh |
| `Error: [message]` | General error occurred | Check error message |

## Responsive Design Notes

- **Desktop/Wide screens:** Dropdown and refresh button appear on the right
- **Small screens:** Layout adapts - dropdown may wrap to new line
- **Portrait orientation:** All elements still accessible
- **Touch-friendly:** Large buttons and dropdown for easy selection

## Accessibility Features

- **Keyboard Navigation:** Tab through ComboBox and Refresh button
- **Screen Reader Support:** All elements have proper XAML labels
- **Color Contrast:** Maintains Windows Theme contrast ratios
- **Focus Indicators:** Visible focus rectangles on interactive elements
