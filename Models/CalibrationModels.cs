using System;

namespace SimRacingPedalCalibrator.Models
{
    public enum ConnectionDeviceType
    {
        DirectInput,    // USB GameControl device
        SerialPort      // COM port (ESP32, Arduino, etc.)
    }

    public enum CurveType
    {
        Linear,         // 1:1 mapping
        Exponential,    // Soft curve (more sensitive at extremes)
        Logarithmic,    // Aggressive curve (more sensitive at center)
        Custom          // User-defined curve
    }

    public class DeviceInfo
    {
        public Guid InstanceGuid { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string InstanceName { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public ConnectionDeviceType ConnectionDeviceType { get; set; } = ConnectionDeviceType.DirectInput;
        public string? ComPort { get; set; }  // For serial ports
        public string DisplayName => $"{ProductName} ({InstanceName})";
        public bool IsConnected { get; set; }
    }

    public class AxisCalibration
    {
        public int Min { get; set; }
        public int Center { get; set; }
        public int Max { get; set; }
        public int DeadZone { get; set; }
        public CurveType CurveType { get; set; } = CurveType.Linear;
        public double CurveStrength { get; set; } = 1.0; // 0.5 to 2.0
        
        // 10-point curve system (11 points from 0.0 to 1.0)
        public double[] CurvePoints { get; set; } = new double[11];

        public AxisCalibration()
        {
            Min = 0;
            Center = 32768;
            Max = 65535;
            DeadZone = 0;
            CurveType = CurveType.Linear;
            CurveStrength = 1.0;
            InitializeLinearCurvePoints();
        }

        public AxisCalibration(int min, int center, int max, int deadZone = 0, CurveType curveType = CurveType.Linear, double curveStrength = 1.0)
        {
            Min = min;
            Center = center;
            Max = max;
            DeadZone = deadZone;
            CurveType = curveType;
            CurveStrength = curveStrength;
            InitializeLinearCurvePoints();
        }

        private void InitializeLinearCurvePoints()
        {
            // Initialize to linear curve (diagonal line)
            for (int i = 0; i < 11; i++)
            {
                CurvePoints[i] = i / 10.0;
            }
        }

        public double Normalize(int rawValue)
        {
            if (rawValue < Min)
                rawValue = Min;
            if (rawValue > Max)
                rawValue = Max;

            // Get normalized value (0.0 to 1.0, with 0.5 being center)
            double normalized;
            if (rawValue < Center)
            {
                normalized = (rawValue - Min) / (double)(Center - Min);
                normalized = normalized * 0.5;
            }
            else
            {
                normalized = (rawValue - Center) / (double)(Max - Center);
                normalized = 0.5 + (normalized * 0.5);
            }

            // Apply 10-point curve interpolation
            return ApplyCurvePoints(normalized);
        }

        private double ApplyCurvePoints(double normalized)
        {
            // Clamp to 0-1 range
            normalized = Math.Max(0, Math.Min(1, normalized));

            // Find which segment we're in
            int segment = (int)(normalized * 10);
            if (segment >= 10) segment = 9;

            double localT = (normalized * 10) - segment;

            // Get 4 points for Catmull-Rom interpolation
            int p0 = Math.Max(0, segment - 1);
            int p1 = segment;
            int p2 = Math.Min(10, segment + 1);
            int p3 = Math.Min(10, segment + 2);

            return CatmullRom(CurvePoints[p0], CurvePoints[p1], CurvePoints[p2], CurvePoints[p3], localT);
        }

        private double CatmullRom(double p0, double p1, double p2, double p3, double t)
        {
            double t2 = t * t;
            double t3 = t2 * t;

            return 0.5 * (
                2.0 * p1 +
                (-p0 + p2) * t +
                (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2 +
                (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3
            );
        }

        public string GetCurveDisplayName()
        {
            return "10-Point Custom Curve";
        }
    }

    public class AxisValues
    {
        public int Brake { get; set; }
        public int Throttle { get; set; }
        public int Clutch { get; set; }
    }

    public class CalibrationData
    {
        public AxisCalibration Brake { get; set; } = new(15500, 16700, 18000);
        public AxisCalibration Throttle { get; set; } = new(13980, 18165, 22350);
        public AxisCalibration Clutch { get; set; } = new(14680, 18705, 22730);
    }
}