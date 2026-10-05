# Device Selection Setup Guide

## ✅ What's New

Your Sim Racing Pedal Calibrator now supports **multiple COM ports and chip sets**! You can:

- 🔄 **Switch between multiple devices** without restarting
- 🔍 **Auto-detect all connected devices** (not just EMCFFBV2)
- 💾 **Maintain separate calibrations** for each device
- 🎮 **Support multiple device types** (Joystick, Gamepad, PedalBox, etc.)

## 🎯 How to Use

### Starting the Application
1. Launch **SimRacingPedalCalibrator.exe**
2. The app automatically scans for connected devices
3. Available devices appear in the dropdown menu at the top

### Selecting a Device
1. Click the **"Select Device"** dropdown
2. Choose your pedal device from the list
3. The app automatically:
   - Connects to the device
   - Loads saved calibration data
   - Starts reading axis values

### Switching Devices
1. Simply select a different device from the dropdown
2. Previous device is automatically disconnected
3. New device calibration is loaded
4. No restart needed!

### Refreshing Devices
1. Click the **"Refresh"** button to rescan
2. Use this if you:
   - Just connected a new device
   - Want to remove a disconnected device from the list
   - Need to troubleshoot device detection

## 📋 Supported Devices

The application now supports any USB device recognized as a **GameControl** device, including:

- ✅ **EMCFFBV2** (Fanatec pedal box)
- ✅ **PedalBox** (various pedal boxes)
- ✅ **Joystick** (racing wheels, joysticks)
- ✅ **Gamepad** (game controllers)
- ✅ Any other USB gaming device

## 🔧 Technical Details

### Device Enumeration
- Scans Windows DirectInput for all connected GameControl devices
- Displays both Product Name and Instance Name for clarity
- Shows device VendorID and ProductID internally

### Calibration Storage
Each device's calibration is stored separately in the Windows Registry:
```
HKEY_CURRENT_USER\System\CurrentControlSet\Control\MediaProperties\PrivateProperties\DirectInput\VID_XXXX&PID_XXXX\Calibration\0\Type\Axes
```

Where `XXXX` is the device's Vendor ID and Product ID in hexadecimal.

### Device Connection
- Properly handles device connection/disconnection
- Prevents multiple simultaneous connections
- Gracefully handles unplugged devices

## ⚙️ Installation & Setup

### System Requirements
- Windows 10 (build 19041) or later
- .NET 8.0 Runtime
- USB port for device connection

### Installation Steps
1. Ensure your pedal device is connected and powered on
2. Run `SimRacingPedalCalibrator.exe`
3. Select your device from the dropdown
4. Begin calibration!

## 🎬 Example Workflow

```
1. Start Application
   ↓
2. "Searching..." → Lists 2 devices:
   - "EMCFFBV2 (Joystick - EMCFFBV2 Pedals)"
   - "Thrustmaster (Joystick - Thrustmaster T500RS)"
   ↓
3. Select EMCFFBV2
   ↓
4. Status: "Connected: EMCFFBV2"
   ↓
5. Calibrate or Load Calibration
   ↓
6. Switch to Thrustmaster
   ↓
7. Automatic Calibration Loading
   ↓
8. Calibrate or Load Calibration
```

## 🐛 Troubleshooting

### "No devices found"
**Solution:**
- Verify device is connected and powered on
- Check Windows Device Manager (Devices and Printers)
- Try unplugging and reconnecting the device
- Click "Refresh" button

### Device not showing in list
**Solution:**
- Device may not be recognized as a GameControl device
- Check Device Manager for correct driver installation
- Verify USB port is working (try different port)
- Some devices need specific drivers from manufacturer

### Calibration data disappears after switching devices
**Solution:**
- This is normal! Each device has separate calibration storage
- The app remembers each device's settings in the registry
- Switch back to the original device to see its calibration

### Application crashes when connecting
**Solution:**
- Update your device drivers
- Try different USB port
- Restart the application
- Check Windows Event Viewer for error details

## 📝 Advanced: Adding Custom Device Support

To add support for a device with a custom name pattern:

1. Edit `Services/DirectInputService.cs`
2. Find the `TargetNames` array:
   ```csharp
   private static readonly string[] TargetNames = { "EMCFFBV2", "EMC", "PedalBox", "Joystick", "Gamepad" };
   ```
3. Add your device name (or part of it):
   ```csharp
   private static readonly string[] TargetNames = { "EMCFFBV2", "EMC", "PedalBox", "Joystick", "Gamepad", "YourDevice" };
   ```
4. Rebuild the application

**Note:** The app actually enumerates ALL connected game control devices now, so custom names are optional for basic functionality.

## 🔐 Data Locations

### Calibration Data
- **Stored in:** Windows Registry (per-device)
- **Path:** `HKEY_CURRENT_USER\System\CurrentControlSet\Control\MediaProperties\PrivateProperties\DirectInput\`
- **Access:** Requires User privileges (no admin needed)

### Application Settings
- **Stored in:** Application folder (no external config files)
- **Portability:** Application is self-contained

## 📞 Support

For issues or questions:
1. Check this guide's Troubleshooting section
2. Verify device is recognized in Windows Device Manager
3. Try the "Refresh" button to rescan devices
4. Check the application's status message for specific errors

## 🚀 Features Coming Soon

Potential future enhancements:
- Serial COM port (RS-232) support for legacy devices
- Custom axis mapping UI
- Device preset profiles
- Firmware information display
- Multi-device calibration backup/restore

---

**Version:** 2.0 (Multi-Device Support)  
**Last Updated:** 2024  
**Build Status:** ✅ Release Build Verified
