using UnityEngine;

namespace ThermalPumpDT.Sensors
{
    /// <summary>Abstract base class for all pump sensors. Tracks live value and alarm state.</summary>
    public abstract class SensorBase
    {
        public string SensorName { get; protected set; }
        public string Unit        { get; protected set; }
        public float  MinValue    { get; protected set; }
        public float  MaxValue    { get; protected set; }
        public float  WarningThresholdHigh { get; protected set; }
        public float  CriticalThresholdHigh { get; protected set; }
        public float  WarningThresholdLow  { get; protected set; }
        public float  CriticalThresholdLow { get; protected set; }

        public float CurrentValue { get; private set; }
        public bool  IsInWarning  { get; private set; }
        public bool  IsInAlarm    { get; private set; }

        public void UpdateValue(float value)
        {
            CurrentValue = Mathf.Clamp(value, MinValue - MaxValue * 0.5f, MaxValue * 1.5f);
            IsInAlarm    = CurrentValue > CriticalThresholdHigh || CurrentValue < CriticalThresholdLow;
            IsInWarning  = !IsInAlarm && (CurrentValue > WarningThresholdHigh || CurrentValue < WarningThresholdLow);
        }

        /// <summary>Normalised value 0-1 for gauge rendering.</summary>
        public float NormalisedValue => Mathf.InverseLerp(MinValue, MaxValue, CurrentValue);
    }

    // ── Concrete sensors ─────────────────────────────────────────────

    public class RPMSensor : SensorBase
    {
        public RPMSensor(float nominalRPM)
        {
            SensorName = "Speed"; Unit = "RPM";
            MinValue = 0; MaxValue = nominalRPM * 1.2f;
            WarningThresholdLow  = nominalRPM * 0.9f;
            WarningThresholdHigh = nominalRPM * 1.05f;
            CriticalThresholdLow = nominalRPM * 0.8f;
            CriticalThresholdHigh = nominalRPM * 1.1f;
        }
    }

    public class FlowSensor : SensorBase
    {
        public FlowSensor(float nominalFlow)
        {
            SensorName = "Flow Rate"; Unit = "m³/h";
            MinValue = 0; MaxValue = nominalFlow * 1.3f;
            WarningThresholdLow  = nominalFlow * 0.78f;
            WarningThresholdHigh = nominalFlow * 1.1f;
            CriticalThresholdLow = nominalFlow * 0.55f;
            CriticalThresholdHigh = nominalFlow * 1.2f;
        }
    }

    public class PressureSensor : SensorBase
    {
        public PressureSensor(string name, float nominalPressure, bool isInlet)
        {
            SensorName = name; Unit = "bar";
            MinValue = 0; MaxValue = nominalPressure * 1.3f;
            if (isInlet)
            {
                WarningThresholdLow   = nominalPressure * 0.6f;
                CriticalThresholdLow  = nominalPressure * 0.3f;
                WarningThresholdHigh  = nominalPressure * 1.4f;
                CriticalThresholdHigh = nominalPressure * 1.6f;
            }
            else
            {
                WarningThresholdHigh  = nominalPressure * 1.12f;
                CriticalThresholdHigh = nominalPressure * 1.18f;
                WarningThresholdLow   = nominalPressure * 0.85f;
                CriticalThresholdLow  = nominalPressure * 0.7f;
            }
        }
    }

    public class TemperatureSensor : SensorBase
    {
        public TemperatureSensor(float nominalTemp)
        {
            SensorName = "Temperature"; Unit = "°C";
            MinValue = 0; MaxValue = nominalTemp * 1.3f;
            WarningThresholdHigh  = nominalTemp * 1.08f;
            CriticalThresholdHigh = nominalTemp * 1.15f;
            WarningThresholdLow   = nominalTemp * 0.5f;
            CriticalThresholdLow  = 0f;
        }
    }

    public class VibrationSensor : SensorBase
    {
        public VibrationSensor(float maxVibration)
        {
            SensorName = "Vibration"; Unit = "mm/s";
            MinValue = 0; MaxValue = maxVibration * 5f;
            WarningThresholdHigh  = maxVibration * 2f;
            CriticalThresholdHigh = maxVibration * 4f;
            WarningThresholdLow   = 0f;
            CriticalThresholdLow  = -1f;
        }
    }
}
