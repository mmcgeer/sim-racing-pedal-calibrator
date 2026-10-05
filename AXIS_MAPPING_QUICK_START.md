# Axis Mapping Quick Start

## What It Does

The **Configure Axes** feature automatically detects all axes on your device and lets you map them to Throttle, Brake, and Clutch—even if your device has unusual axis names or configurations.

## Quick Setup (3 steps)

### Step 1: Select Your Device
1. Launch the app
2. Select your device from the dropdown (e.g., "Arduino - COM3" or "Fanatec CSL Elite")

### Step 2: Configure Axes
1. Click **"Configure Axes"** button
2. See what axes your device has detected
3. For each pedal (Throttle, Brake, Clutch), select which device axis it uses:
   - **Throttle** → X, Y, Z, RX, RY, RZ, Slider1, etc.
   - **Brake** → X, Y, Z, RX, RY, RZ, Slider1, etc.
   - **Clutch** → X, Y, Z, RX, RY, RZ, Slider1, etc.
   - (Can also select "None" if you only use 2 pedals)

### Step 3: Save & Done!
1. Move your pedals to verify the progress bars update correctly
2. Click **"Save"**
3. Your configuration is automatically saved and will be remembered

## Examples

### ✅ Standard USB Joystick
```
Device has: X, Y, Z, RX, RY, RZ
Map as:
  Throttle → X
  Brake → Y
  Clutch → Z
```

### ✅ Arduino with Pedals
```
Device has: Throttle, Brake, Z Slider
Map as:
  Throttle → Throttle
  Brake → Brake
  Clutch → Z Slider
```

### ✅ Fanatec Wheel Base
```
Device has: X, Y, Z, RX, RY, RZ
Map as:
  Throttle → RZ (right pedal)
  Brake → RX (center pedal)
  Clutch → RY (left pedal)
```

## Troubleshooting

### "No devices found"
- Check device is plugged in
- Windows can see it in Device Manager
- Click "Refresh" to rescan
- Restart the app if device was plugged in after launch

### Axes not updating in Configure dialog
- Move your pedals/axes slowly
- Wait 1-2 seconds for values to update (updates every 100ms)
- Ensure axis is moving through full range (0 to max)

### Can't find my axis
- Close Configure Axes dialog
- Manually test the device in Windows control panel
- Check what axes Windows reports
- Some devices don't expose all axes the same way
- Try selecting each axis one by one to find your pedal

### Configuration not saved
- Click "Save" button in Configure Axes dialog
- Should see confirmation message
- Close and reopen app to verify it saved

### Still using old axis configuration
- Device mapping changed?
- Click "Configure Axes" again
- Update the mapping
- Click "Save"
- Mapping is per-device, so different devices keep different configs

## What Gets Saved?

When you click "Save", the app remembers:
- Which device axis maps to Throttle
- Which device axis maps to Brake
- Which device axis maps to Clutch
- Device name and when you configured it

This is stored in Windows Registry under:
```
HKEY_CURRENT_USER\Software\SimRacingPedalCalibrator\AxisMappings
```

## Can I Use Multiple Devices?

**Yes!** Each device gets its own configuration:
1. Configure Device A → Save
2. Configure Device B → Save
3. Switch between them
4. Each device automatically loads its saved mapping

## What If I Connect a New Device?

1. Plug in device
2. Click "Refresh" in app
3. Select new device from dropdown
4. Click "Configure Axes"
5. New device gets auto-detected axes
6. Configure and save
7. That device is now remembered

## Common Axis Names

| Abbreviation | Meaning | Common Use |
|---|---|---|
| X | Horizontal axis | Steering wheel rotation |
| Y | Vertical axis | Forward/backward |
| Z | Depth axis | Throttle/Brake pedals |
| RX | Rotation X | Heel/toe pedal |
| RY | Rotation Y | Side-to-side pedal |
| RZ | Rotation Z | Pedal axis #3 |
| Slider1 | First slider/potentiometer | Extra pedal input |
| Slider2 | Second slider/potentiometer | Extra pedal input |
| Throttle | Named throttle axis | Arduino custom name |
| Brake | Named brake axis | Arduino custom name |
| Clutch | Named clutch axis | Arduino custom name |

## Still Need Help?

The Configure Axes dialog shows:
- Device name
- Number of axes detected
- Current value of each axis (updates as you move pedals)
- Progress bars showing axis position

Use this to verify your device is working before mapping it!

---

**Updated:** 2024 | **Feature:** Flexible Axis Mapping System
