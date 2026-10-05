# Sim Racing Pedal Calibrator v1.0 - Release Notes

## 🎉 **First Official Release!**

This is the enhanced version of [Adrian de Malmanche's](https://github.com/Adriandemalmanche/sim-racing-pedal-calibrator) original Sim Racing Pedal Calibrator, featuring three major enhancements.

---

## ✨ **Major Features**

### 🎚️ Professional 10-Point Response Curves (v2.0)
- Interactive drag-and-drop control points for complete curve customization
- Catmull-Rom spline interpolation for smooth, natural transitions
- **4 preset curves**: Racing Brake, Smooth Throttle, Precise Steering, Linear
- Save/load unlimited custom curve profiles
- Real-time deviation metrics showing curve sensitivity

### 🔄 Flexible Device Axis Mapping (v3.0)
- Automatic detection of device axes (DirectInput and Serial)
- Interactive mapping dialog with real-time feedback
- Works with ANY device configuration (not just X/Y/Z)
- Examples: Fanatec wheels, Arduino controllers, custom hardware
- Per-device configuration persistence via Windows Registry

### 📊 Real-Time Pedal Output Display
- Live visualization of input → curve → output transformation
- Color-coded progress bars (blue input, green output)
- Percentage display (0-100%)
- Helps tune curves to perfect feel

### 🎯 Core Features (Original)
- Live graphing of raw pedal values
- Interactive calibration (press pedals to auto-detect ranges)
- Dead zone adjustment per axis
- Windows Registry integration
- Modern WinUI 3 interface

---

## 📦 **Package Contents**

```
SimRacingPedalCalibrator-v1.0.zip (115 MB)
├── SimRacingPedalCalibrator.exe        (main application)
├── Supporting DLLs                     (included)
├── Configuration files
└── All dependencies (self-contained)
```

**No installation required!** Just extract and run.

---

## 🚀 **Quick Start**

1. **Extract the zip file**
2. **Run `SimRacingPedalCalibrator.exe`**
3. Select your device
4. *(Optional)* Click "Configure Axes" to map device axes to pedals
5. Calibrate by moving pedals through full range
6. *(Optional)* Click "Response Curve" tab to fine-tune curve
7. Click "Save Calibration"

---

## 🛠️ **System Requirements**

| Requirement | Details |
|---|---|
| OS | Windows 11 (build 19041+) |
| .NET | Included (self-contained) |
| RAM | 100 MB minimum |
| Storage | 150 MB for application |
| USB | DirectInput compatible device |

---

## 📚 **Documentation**

Complete documentation available in the repository:

### User Guides
- [AXIS_MAPPING_QUICK_START.md](https://github.com/mmcgeer/sim-racing-pedal-calibrator/blob/main/AXIS_MAPPING_QUICK_START.md)
- [CURVE_QUICK_REFERENCE.md](https://github.com/mmcgeer/sim-racing-pedal-calibrator/blob/main/CURVE_QUICK_REFERENCE.md)
- [README.md](https://github.com/mmcgeer/sim-racing-pedal-calibrator/blob/main/README.md)

### Technical Documentation
- [AXIS_MAPPING_GUIDE.md](https://github.com/mmcgeer/sim-racing-pedal-calibrator/blob/main/AXIS_MAPPING_GUIDE.md)
- [AXIS_MAPPING_ARCHITECTURE.md](https://github.com/mmcgeer/sim-racing-pedal-calibrator/blob/main/AXIS_MAPPING_ARCHITECTURE.md)
- [IMPLEMENTATION_SUMMARY_AXIS_MAPPING.md](https://github.com/mmcgeer/sim-racing-pedal-calibrator/blob/main/IMPLEMENTATION_SUMMARY_AXIS_MAPPING.md)

---

## 🎮 **Supported Devices**

### DirectInput (USB)
✅ Fanatec Podium/ClubSport wheels
✅ Thrustmaster T300/T500/TX wheels
✅ Logitech G wheels
✅ Any USB joystick/gamepad with axes

### Serial (COM Port)
✅ Arduino & compatibles
✅ ESP32 & microcontrollers
✅ Custom serial pedal controllers

### Automatic Axis Detection
Works with any axis configuration - not limited to X/Y/Z!

---

## 🔄 **Version History**

| Version | Features | Date |
|---|---|---|
| **v1.0** | 10-point curves, axis mapping, pedal output display | 2024 |
| v2.0 | Response curve system | Earlier |
| v1.0 | Original calibrator | Original |

---

## 👏 **Credits**

**Original Project**: [Adrian de Malmanche](https://github.com/Adriandemalmanche/sim-racing-pedal-calibrator)

**Enhancements & Development**: 
- 10-point interactive response curves
- Flexible device axis mapping system
- Real-time pedal output display
- AI-assisted development (Copilot)

---

## 🐛 **Known Issues**

1. DirectInput axis detection uses index-based mapping
   - **Workaround**: Use "Configure Axes" dialog to manually map if needed

2. Serial devices limited to 3 axes
   - **Workaround**: Update firmware to transmit more axes

3. Requires restart to apply new axis mapping
   - **Planned**: Hot-reload support

---

## 💡 **Tips & Tricks**

### Getting the Best Results
1. **Calibrate first** with linear curve
2. **Test your device** axes are mapped correctly
3. **Use presets** as starting points
4. **Adjust gradually** - small curve changes make big differences
5. **Save profiles** for different games/setups

### Common Scenarios
- **Racing Brakes**: Use "Racing Brake" preset → adjust to taste
- **Smooth Throttle**: Use "Smooth Throttle" preset → dial in sensitivity
- **Precise Steering**: Use "Precise Steering" preset → perfect for sim

---

## 📋 **What's Next?**

Future enhancements (not yet implemented):
- [ ] Custom axis naming
- [ ] Per-game automatic profile switching
- [ ] Button/trigger mapping
- [ ] Curve import/export
- [ ] Advanced axis filtering

---

## 🤝 **Contributing**

Want to contribute? 
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a Pull Request

---

## 📄 **License**

MIT License - See LICENSE file for details

---

**Enjoy your enhanced sim racing calibration! 🏁**

For issues or questions, check the documentation or open an issue on GitHub.
