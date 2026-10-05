# Real-Time Pedal Output Display Feature

## Overview

The Response Curve screen now displays **live pedal output** in real-time, allowing you to see exactly how your response curve is transforming your input as you move the pedals.

## What It Shows

When you're on the **"Response Curve"** tab:

### 📊 Live Output Section (Right Side of Curve)

You'll see a panel with:

1. **Input %** (Blue)
   - Current pedal position as a percentage (0-100%)
   - Shows your raw pedal input
   - Updates in real-time as you move pedal

2. **Output %** (Green)
   - How your curve transforms that input
   - The value being sent to your game/application
   - Shows the effect of your curve

3. **Progress Bars**
   - Blue bar: Shows input level
   - Green bar: Shows output level
   - Side-by-side comparison makes it easy to see curve effect

## How to Use It

### Scenario 1: Testing a Preset Curve

```
1. Move to Response Curve tab
2. Click a preset (e.g., "Racing Brake")
3. Move your pedal slowly
4. Watch Input % go from 0-100%
5. Watch Output % transform based on the curve
   - Linear preset: Input % = Output %
   - Racing Brake: Output % jumps faster at start
   - Smooth Throttle: Output % is lower at start
```

### Scenario 2: Creating Custom Curve

```
1. Drag points on the curve to your preference
2. Move pedal to test
3. Watch Output % to see how curve transforms your input
4. Fine-tune curve points until output feels right
5. Save your custom profile
```

### Scenario 3: Comparing Input vs Output

```
1. Set up a curve
2. Move pedal halfway (Input = 50%)
3. Look at Output % - it will differ based on curve
4. This shows exactly how aggressive/smooth your curve is
```

## Examples

### Linear Curve (1:1 Mapping)
```
Input:  0%   → Output: 0%
Input: 25%   → Output: 25%
Input: 50%   → Output: 50%
Input: 75%   → Output: 75%
Input: 100%  → Output: 100%
```

### Racing Brake (Aggressive)
```
Input:  0%   → Output: 0%
Input: 10%   → Output: 25%  ← Early response!
Input: 50%   → Output: 80%
Input: 100%  → Output: 100%
```

### Smooth Throttle (Gentle)
```
Input:  0%   → Output: 0%
Input: 25%   → Output: 5%   ← Delayed response
Input: 50%   → Output: 50%
Input: 100%  → Output: 100%
```

## Visual Design

```
┌─────────────────────────────────────────────┐
│ Interactive 10-Point Curve                  │
│ [Preset buttons: Racing Brake | Smooth...] │
│                                              │
│  ┌────────────────┐  ┌──────────────────┐  │
│  │                │  │   Live Output    │  │
│  │                │  │                  │  │
│  │    Curve       │  │  Input:   45%    │  │
│  │    Graph       │  │  ──→             │  │
│  │                │  │  Output:  38%    │  │
│  │                │  │                  │  │
│  │  (drag points) │  │ [Blue  progress] │  │
│  │                │  │ [Green progress] │  │
│  └────────────────┘  └──────────────────┘  │
│                                              │
│  Linear curve - drag points to modify       │
│  Deviation: 0%                              │
│                                              │
│  [Reset] [Save Profile] [Load Profile]     │
└─────────────────────────────────────────────┘
```

## Technical Details

### Real-Time Updates
- Updates every frame when pedal values change
- Uses the same polling mechanism as main display
- Synchronized with calibration data

### Curve Application
- Takes normalized input (0-1)
- Applies Catmull-Rom spline interpolation
- Clamps output to valid range
- Displays as percentage (0-100%)

### Performance
- Minimal overhead (single interpolation per update)
- No impact on curve dragging or other operations
- Smooth real-time display at 60 FPS

## Tips & Tricks

### Finding Your Perfect Curve

1. **Start with presets** - Use Racing Brake, Smooth Throttle, or Precise Steering
2. **Watch the output** - Move pedal slowly and watch Output % response
3. **Feel vs Numbers** - Some curves are better "felt" than calculated
4. **Save and test** - Save profiles and test in your game
5. **Iterate** - Small adjustments to curve points can make big differences

### Common Adjustments

**Want more aggressive response at start?**
- Drag point at 10% input higher (increase its output %)
- Watch Output % jump faster for small pedal movements

**Want more control at low speeds?**
- Use Smooth Throttle preset
- Output % will be lower for small pedal movements

**Want perfect 1:1 mapping?**
- Click "Linear" preset
- Input % = Output % exactly

## Troubleshooting

### Output not updating?
- Make sure pedal is actually moving
- Check if calibration min/max are correct
- Output can only show values between your min/max

### Output frozen at one value?
- Check if pedal is at rest position
- Output will be the same until you move pedal
- Try moving pedal through full range

### Output looks different than expected?
- Verify calibration is correct (Calibration tab)
- Check if curve points are where you think they are
- Try resetting to Linear first to verify baseline

## What's New in This Release

✨ **Live Output Display** - See real-time curve transformation
✨ **Progress Bars** - Visual comparison of input vs output
✨ **Percentage Display** - Easy-to-read 0-100% format
✨ **Color Coding** - Blue for input, Green for output
✨ **Instant Feedback** - See effect of curve changes immediately

---

**Feature: Real-Time Pedal Output Display** | **Version 3.1** | **2024**
