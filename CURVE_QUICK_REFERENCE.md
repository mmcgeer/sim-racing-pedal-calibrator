# Quick Reference: 10-Point Curve System

## 🎯 At a Glance

| Feature | Details |
|---------|---------|
| **Control Points** | 11 draggable points (0.0 to 1.0) |
| **Presets** | Racing Brake, Smooth Throttle, Precise Steering, Linear |
| **Profiles** | Save/load unlimited custom curves |
| **Interpolation** | Catmull-Rom spline (professional smoothing) |
| **Visual Feedback** | Real-time curve + deviation % |
| **Storage** | Windows local app data (persistent) |

---

## 🎮 Quick Start (30 seconds)

1. Open app → Select axis (Throttle/Brake/Clutch)
2. Click **Response Curve** tab
3. Click any preset button (Racing Brake, Smooth Throttle, etc.)
4. Drag blue points to customize
5. Click **💾 Save Profile** to store
6. Use dropdown to load saved profiles

---

## 🏎️ Preset Curves

### Racing Brake
**Use when**: Need aggressive early brake response  
**Characteristics**: Very sensitive at 10% input (0.25)  
**Deviation**: ~22%  
**Best for**: Threshold braking, sim racing  

### Smooth Throttle  
**Use when**: Want gentle acceleration  
**Characteristics**: Low sensitivity at start (0.05)  
**Deviation**: ~12%  
**Best for**: Smooth control, fuel efficiency  

### Precise Steering
**Use when**: Need balanced mid-range sensitivity  
**Characteristics**: S-curve with peak in middle  
**Deviation**: ~15%  
**Best for**: Steering wheels, fine control  

### Linear
**Use when**: Want no curve (1:1 mapping)  
**Characteristics**: Perfect diagonal line  
**Deviation**: 0%  
**Best for**: Testing, baseline  

---

## 💡 Tips

| Tip | Why |
|-----|-----|
| Start with presets | Gives you good starting points |
| Watch deviation % | Shows how customized your curve is |
| Drag middle points most | More range for fine-tuning |
| Save multiple versions | Compare different approaches |
| Use clear names | "Brake-RaceMode" vs "B1" |
| Reset before trying new preset | Clean slate for comparison |

---

## 🎯 Common Tasks

### Save Your Custom Curve
```
1. Adjust points until satisfied
2. Click "💾 Save Profile"
3. Enter name: "Throttle - My Setup"
4. Click "Save" in dialog
✅ Done! Profile saved
```

### Load a Saved Profile
```
1. Click dropdown below canvas
2. Select profile name
3. Curve instantly updates
✅ Done! Profile loaded
```

### Compare Two Curves
```
1. Load first profile
2. Observe curve shape
3. Load second profile  
4. Compare deviation %
5. Note differences
✅ Done! A/B testing complete
```

### Reset to Linear
```
1. Click "Reset to Linear" button
2. Curve returns to diagonal
3. Deviation shows 0%
✅ Done! Back to baseline
```

---

## 📊 Deviation % Guide

| Deviation | Curve Type | Intensity |
|-----------|-----------|-----------|
| 0% | Linear | None |
| 5-10% | Very Gentle | Subtle |
| 10-15% | Gentle | Mild |
| 15-25% | Moderate | Noticeable |
| 25%+ | Aggressive | Strong |
| 35%+ | Very Aggressive | Extreme |

*Use deviation % to judge how "bent" your curve is from linear*

---

## 🔧 Control Points Explained

```
Position 0 (0.0) - Always 0.0  ← Input at 0%
Position 1 (0.1)
Position 2 (0.2)
Position 3 (0.3)
Position 4 (0.4)
Position 5 (0.5) - Midpoint
Position 6 (0.6)
Position 7 (0.7)
Position 8 (0.8)
Position 9 (0.9)
Position 10 (1.0) - Always 1.0  ← Input at 100%

* Drag points UP = LESS sensitive at that position
* Drag points DOWN = MORE sensitive at that position
* First and last points locked (can't drag)
```

---

## 💾 Profile Storage

- **Stored in**: Windows Local App Data
- **Survives**: App updates, restarts, uninstalls
- **Accessible**: Only by this app
- **Format**: Simple text (ProfileName=Point1,Point2,...)
- **Limit**: Unlimited profiles
- **Size**: ~80 bytes per profile

---

## ❓ Troubleshooting

| Issue | Solution |
|-------|----------|
| Preset button doesn't work | Make sure you're on Response Curve tab |
| Can't save profile | Enter a profile name in dialog |
| Profile not in dropdown | Restart app or click dropdown to refresh |
| Curve looks wrong | Click "Reset to Linear" and try again |
| Points not dragging | Click on point first to select it |

---

## 🚀 Pro Tips

1. **Create a baseline profile** - Save your first good setup as "Baseline-AxisName"
2. **Iterate carefully** - Save each variation with different names
3. **Use consistent naming** - "Brake-Aggressive", "Brake-Smooth" makes sense
4. **Test in game** - Always verify curve works in your sim
5. **Document what works** - Note which profiles feel best
6. **Keep library updated** - Remove old unused profiles occasionally
7. **Share favorites** - Remember your best profile names for next session

---

## 📞 Support

**Something not working?**
- Reset the app: Close and reopen
- Reset curve: Click "Reset to Linear"
- Check Windows permissions: App data folder must be accessible
- Try simplifying profile name: No special characters except hyphens/spaces

**Want to know more?**
- See CURVE_SYSTEM_FEATURES.md for full documentation
- Each section has detailed examples
- Workflows show step-by-step processes

---

**Happy tuning! 🏎️**  
*Professional-grade pedal customization at your fingertips*
