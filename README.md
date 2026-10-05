# Sim Racing Pedal Calibrator

A modern Windows 11 calibration application for sim racing pedals. Features real-time raw value reading, interactive calibration, professional response curves, and flexible device axis mapping.

## ✨ Features

### Core Features
- 📊 **Live Graphing** - Visualize pedal values in real-time
- 🎛️ **Interactive Calibration** - Press pedals to auto-detect min/max/center values
- 💾 **Registry Integration** - Load and save calibration data to Windows Registry
- 🎨 **Modern UI** - Built with WinUI 3 for a native Windows 11 experience
- ⌨️ **Dead Zone Control** - Fine-tune dead zones for each axis

### New: Professional Response Curves (v2)
- 🎚️ **10-Point Interactive Curves** - Drag control points to create custom response curves
- 📈 **4 Preset Curves** - Racing Brake, Smooth Throttle, Precise Steering, Linear
- 📊 **Catmull-Rom Spline Interpolation** - Smooth, natural curve transitions
- 💾 **Profile System** - Save and load custom curve profiles with persistent storage
- 📐 **Deviation Metrics** - Real-time calculation of curve deviation percentage
- ↩️ **Reset to Linear** - Quickly return to 1:1 mapping for comparison

### New: Flexible Axis Mapping (v3)
- 🔄 **Automatic Axis Detection** - Detects all available axes on any device
- 🎮 **Device-Agnostic** - Works with any DirectInput device or serial controller
- ⚙️ **Interactive Mapping Dialog** - Configure which device axis maps to which pedal
- 🔍 **Real-Time Feedback** - Progress bars show live axis values during configuration
- 💾 **Per-Device Persistence** - Each device remembers its axis configuration
- 🔌 **Multi-Device Support** - Use different devices with different axis configs

## 🚀 Quick Start

### For Users
1. Launch the application
2. Select your device from the dropdown
3. **(New)** Click "Configure Axes" to map device axes to pedals (if needed)
4. Calibrate each pedal by moving them through full range
5. **(New)** Click on "Response Curve" tab to adjust curve if desired
6. Click "Save Calibration"

### For Developers
See [IMPLEMENTATION_SUMMARY_AXIS_MAPPING.md](IMPLEMENTATION_SUMMARY_AXIS_MAPPING.md) for technical implementation details.

## 📋 Documentation

### User Guides
- [AXIS_MAPPING_QUICK_START.md](AXIS_MAPPING_QUICK_START.md) - Quick reference for axis configuration (new!)
- [CURVE_QUICK_REFERENCE.md](CURVE_QUICK_REFERENCE.md) - Quick reference for response curves

### Technical Documentation
- [AXIS_MAPPING_GUIDE.md](AXIS_MAPPING_GUIDE.md) - Comprehensive axis mapping implementation guide
- [AXIS_MAPPING_ARCHITECTURE.md](AXIS_MAPPING_ARCHITECTURE.md) - Technical architecture and design patterns
- [CURVE_SYSTEM_FEATURES.md](CURVE_SYSTEM_FEATURES.md) - Detailed curve system documentation
- [IMPLEMENTATION_SUMMARY_AXIS_MAPPING.md](IMPLEMENTATION_SUMMARY_AXIS_MAPPING.md) - Implementation summary with impact assessment

## 🎯 Supported Devices

### DirectInput Devices
- Any USB joystick, gamepad, or wheel base
- Auto-detects all available axes (X, Y, Z, RX, RY, RZ, Sliders, etc.)
- Examples: Fanatec wheels, Thrustmaster, Logitech, generic USB controllers

### Serial Devices (COM Port)
- Arduino and compatible microcontrollers
- ESP32 and other serial-based controllers
- Custom pedal controllers using serial protocol

### Automatic Axis Mapping
- Detects axes without manual configuration
- User can customize mapping via "Configure Axes" dialog
- Saves configuration per device for easy reconnection

## 🛠️ Technology Stack

- **Framework**: .NET 8 with WinUI 3
- **Device Communication**: SharpDX DirectInput & System.IO.Ports
- **Graphing**: XAML Canvas with custom curve rendering
- **Interpolation**: Catmull-Rom spline algorithm
- **Persistence**: Windows Registry + Local ApplicationData
- **UI**: WinUI 3 with XAML

## 📥 Installation

### From Source
1. Clone the repository
2. Open `SimRacingPedalCalibrator.sln` in Visual Studio 2022+
3. Restore NuGet packages
4. Build Release configuration
5. Run executable from `bin\Release\net8.0-windows10.0.19041\`

### Prerequisites
- Windows 11 with 19041+ build
- .NET 8 SDK or later
- Visual Studio 2022 (optional, can use `dotnet` CLI)

## 💡 Usage Examples

### Example 1: Standard USB Joystick
```
1. Plug in Fanatec/Thrustmaster wheel
2. Launch app → Select device
3. (Optional) Configure Axes if axis order is different
4. Calibrate: Move throttle fully → Move brake fully → Move clutch fully
5. Save
```

### Example 2: Arduino Pedals
```
1. Upload pedal firmware to Arduino
2. Connect USB
3. Launch app → Select "Arduino - COM3" (or your port)
4. Configure Axes:
   - Throttle → Throttle (Index 0)
   - Brake → Brake (Index 1)
   - Clutch → Z Slider (Index 2)
5. Save mapping
6. Calibrate pedals
7. Save calibration
```

### Example 3: Custom Response Curve
```
1. Calibrate pedals (standard process)
2. Click "Response Curve" tab
3. Click "Racing Brake" preset (or drag points for custom)
4. Adjust points until curve matches your feel preference
5. Deviation shows % difference from linear
6. Click "Save Profile" to remember this curve
7. Curve auto-applies on next launch
```

## 🔧 Configuration

### Calibration Storage
- Location: `HKEY_CURRENT_USER\System\CurrentControlSet\Control\MediaProperties\PrivateProperties\DirectInput\...`
- Format: Hex values for min/center/max per axis

### Curve Profiles
- Location: `HKEY_CURRENT_USER\AppData\Local\...\SimRacingPedalCalibrator\`
- Format: Pipe-delimited profile names, comma-delimited curve points

### Axis Mappings
- Location: `HKEY_CURRENT_USER\Software\SimRacingPedalCalibrator\AxisMappings\`
- Per-device storage with mapping configuration

## 🚦 System Requirements

| Requirement | Details |
|---|---|
| OS | Windows 11 (build 19041+) |
| .NET | .NET 8 SDK |
| RAM | 100 MB minimum |
| Storage | 50 MB for application |
| USB | DirectInput compatible device |

## 📊 Version History

### v3.0 - Flexible Axis Mapping (Current)
- ✅ Automatic axis detection
- ✅ Interactive mapping configuration
- ✅ Per-device persistence
- ✅ Real-time feedback during configuration
- ✅ Multi-device support
- ✅ Full documentation suite

### v2.0 - Response Curves
- ✅ 10-point interactive curve editor
- ✅ Catmull-Rom spline interpolation
- ✅ 4 preset curve templates
- ✅ Profile save/load system
- ✅ Deviation metric calculation

### v1.0 - Initial Release
- ✅ Basic calibration system
- ✅ Min/center/max calibration
- ✅ Dead zone adjustment
- ✅ Registry persistence

## 🐛 Known Issues

1. DirectInput axis detection uses index-based mapping
   - Workaround: Manually configure via "Configure Axes" dialog if axes are in unexpected order
   
2. Serial port axes limited to 3 axes
   - Workaround: Firmware update to support more axes

3. Requires application restart to apply new axis mapping
   - Planned: Hot-reload axis mapping without restart

## 🔮 Roadmap

### Upcoming Features
- [ ] Custom axis naming for clarity
- [ ] Axis range per-device calibration
- [ ] Quick preset configurations (e.g., "Fanatec Default Setup")
- [ ] Export/import mapping profiles
- [ ] Per-game automatic profile switching
- [ ] Button and trigger support
- [ ] Advanced axis filtering
- [ ] Curve import/export in common formats

## 📝 License

MIT - See LICENSE file for details

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## 📞 Support

- Check [AXIS_MAPPING_QUICK_START.md](AXIS_MAPPING_QUICK_START.md) for common questions
- Review detailed [AXIS_MAPPING_GUIDE.md](AXIS_MAPPING_GUIDE.md)
- See [IMPLEMENTATION_SUMMARY_AXIS_MAPPING.md](IMPLEMENTATION_SUMMARY_AXIS_MAPPING.md) for technical details

---

**Latest Update**: Flexible Axis Mapping System (v3.0) - 2024
