using System.Collections.Generic;
using UnityEngine;
using ThermalPumpDT.Core;

namespace ThermalPumpDT.Anomaly
{
    /// <summary>
    /// Rule-based anomaly detector.  Evaluates pump state every tick and
    /// raises AnomalyEvents via TwinManager when faults are detected.
    /// Implements a cool-down to avoid flooding the alert system.
    /// </summary>
    public class AnomalyDetector : MonoBehaviour
    {
        [Header("Cooldown (seconds) between repeated alerts per fault")]
        public float AlertCooldown = 30f;

        // Track last alert time per (pumpId, faultType)
        private readonly Dictionary<string, float> _lastAlertTime = new();

        private void OnEnable()  => TwinManager.OnAnyPumpStateUpdated += Evaluate;
        private void OnDisable() => TwinManager.OnAnyPumpStateUpdated -= Evaluate;

        private void Evaluate(PumpTwin twin, PumpState s)
        {
            var nom = twin.Nominals;

            // ── Cavitation ──────────────────────────────────────────
            if (s.InletPressure < nom.NominalInletPressure * 0.45f && s.VibrationRMS > nom.MaxVibration * 2.5f)
                TryRaise(twin, FaultType.Cavitation, FaultSeverity.Critical,
                    $"Cavitation detected on {twin.PumpName}: inlet P={s.InletPressure:F2} bar, vib={s.VibrationRMS:F1} mm/s");

            // ── Overheating ─────────────────────────────────────────
            if (s.Temperature > nom.NominalTemperature * 1.1f)
            {
                var sev = s.Temperature > nom.NominalTemperature * 1.15f ? FaultSeverity.Critical : FaultSeverity.Warning;
                TryRaise(twin, FaultType.Overheating, sev,
                    $"High temperature on {twin.PumpName}: {s.Temperature:F1}°C");
            }

            // ── Bearing Wear ────────────────────────────────────────
            if (s.VibrationRMS > nom.MaxVibration * 3f)
                TryRaise(twin, FaultType.BearingWear, FaultSeverity.Critical,
                    $"Bearing wear on {twin.PumpName}: vibration={s.VibrationRMS:F1} mm/s");
            else if (s.VibrationRMS > nom.MaxVibration * 2f)
                TryRaise(twin, FaultType.BearingWear, FaultSeverity.Warning,
                    $"Elevated vibration on {twin.PumpName}: {s.VibrationRMS:F1} mm/s");

            // ── Seal Leak ───────────────────────────────────────────
            if (s.FlowRate < nom.NominalFlow * 0.65f && s.RPM > nom.NominalRPM * 0.9f)
                TryRaise(twin, FaultType.SealLeak, FaultSeverity.Warning,
                    $"Possible seal leak on {twin.PumpName}: flow={s.FlowRate:F0} m³/h at {s.RPM:F0} RPM");

            // ── Dead-head ───────────────────────────────────────────
            if (s.FlowRate < 5f && s.RPM > nom.NominalRPM * 0.8f)
                TryRaise(twin, FaultType.DeadHead, FaultSeverity.Critical,
                    $"Dead-head condition on {twin.PumpName}: flow=0 while running at {s.RPM:F0} RPM");

            // ── Low health ──────────────────────────────────────────
            if (s.HealthScore < 40f)
                TryRaise(twin, FaultType.ImpellerWear, FaultSeverity.Warning,
                    $"Low health score on {twin.PumpName}: {s.HealthScore:F0}%");
        }

        private void TryRaise(PumpTwin twin, FaultType fault, FaultSeverity severity, string desc)
        {
            string key = $"{twin.PumpId}_{fault}";
            if (_lastAlertTime.TryGetValue(key, out float last) && Time.time - last < AlertCooldown)
                return;
            _lastAlertTime[key] = Time.time;
            TwinManager.Instance?.RaiseAnomaly(new AnomalyEvent(twin.PumpId, fault, severity, desc));
        }
    }
}
