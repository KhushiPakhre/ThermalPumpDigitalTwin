using System;
using UnityEngine;

namespace ThermalPumpDT.Core
{
    public enum PumpType
    {
        BoilerFeedWater,
        CondenserExtraction,
        CoolingWater
    }

    public enum PumpStatus
    {
        Offline, Normal, Warning, Critical
    }

    [Serializable]
    public struct PumpState
    {
        public float RPM;
        public float FlowRate;
        public float InletPressure;
        public float OutletPressure;
        public float Temperature;
        public float VibrationRMS;
        public float EfficiencyPercent;
        public float HealthScore;
        public float RunHours;
        public PumpStatus Status;
        public string Timestamp;
    }

    [Serializable]
    public class PumpNominals
    {
        public float NominalRPM;
        public float NominalFlow;
        public float NominalInletPressure;
        public float NominalOutletPressure;
        public float NominalTemperature;
        public float MaxVibration;
        public float NominalEfficiency;

        public static PumpNominals For(PumpType type)
        {
            switch (type)
            {
                case PumpType.BoilerFeedWater:
                    return new PumpNominals { NominalRPM = 2950, NominalFlow = 900, NominalInletPressure = 4f, NominalOutletPressure = 165f, NominalTemperature = 170f, MaxVibration = 3f, NominalEfficiency = 85f };
                case PumpType.CondenserExtraction:
                    return new PumpNominals { NominalRPM = 1475, NominalFlow = 400, NominalInletPressure = 0.8f, NominalOutletPressure = 12f, NominalTemperature = 45f, MaxVibration = 2.8f, NominalEfficiency = 80f };
                default:
                    return new PumpNominals { NominalRPM = 740, NominalFlow = 8000, NominalInletPressure = 1f, NominalOutletPressure = 4f, NominalTemperature = 32f, MaxVibration = 4f, NominalEfficiency = 78f };
            }
        }
    }

    /// <summary>Core digital-twin model for one pump. Holds live state and computes derived KPIs.</summary>
    public class PumpTwin
    {
        public string PumpId   { get; private set; }
        public string PumpName { get; private set; }
        public PumpType Type   { get; private set; }
        public PumpNominals Nominals { get; private set; }
        public PumpState State { get; private set; }

        public event Action<PumpState> OnStateUpdated;
        public event Action<PumpStatus, PumpStatus> OnStatusChanged;

        private PumpStatus _lastStatus = PumpStatus.Offline;
        private float _degradation = 0f;

        public PumpTwin(string id, string name, PumpType type)
        {
            PumpId = id; PumpName = name; Type = type;
            Nominals = PumpNominals.For(type);
            State = new PumpState { Status = PumpStatus.Offline, HealthScore = 100f };
        }

        public void UpdateState(float rpm, float flow, float inletP, float outletP,
                                float temp, float vibration, float runHours, float degradationDelta)
        {
            _degradation = Mathf.Clamp01(_degradation + degradationDelta);
            float efficiency = ComputeEfficiency(rpm, flow, inletP, outletP);
            float health     = ComputeHealthScore(efficiency, vibration, temp, _degradation);
            PumpStatus status = ComputeStatus(temp, vibration, inletP, flow, rpm, health);

            State = new PumpState
            {
                RPM = rpm, FlowRate = flow, InletPressure = inletP, OutletPressure = outletP,
                Temperature = temp, VibrationRMS = vibration, EfficiencyPercent = efficiency,
                HealthScore = health, RunHours = runHours, Status = status,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            OnStateUpdated?.Invoke(State);
            if (status != _lastStatus) { OnStatusChanged?.Invoke(_lastStatus, status); _lastStatus = status; }
        }

        public void PerformMaintenance() { _degradation = 0f; Debug.Log($"[PumpTwin] Maintenance on {PumpName}."); }

        private float ComputeEfficiency(float rpm, float flow, float inletP, float outletP)
        {
            if (Nominals.NominalRPM < 1f) return 0f;
            float rpmRatio = rpm / Nominals.NominalRPM;
            float flowRatio = flow / Nominals.NominalFlow;
            float pressureRise = Mathf.Max(0f, outletP - inletP);
            float nomPress = Nominals.NominalOutletPressure - Nominals.NominalInletPressure;
            if (nomPress < 0.01f) return Nominals.NominalEfficiency;
            float eta = (flowRatio * pressureRise) / (rpmRatio * nomPress + 0.001f);
            return Mathf.Clamp(eta * Nominals.NominalEfficiency, 0f, 100f);
        }

        private float ComputeHealthScore(float efficiency, float vibration, float temp, float deg)
        {
            float eff  = Mathf.Clamp01(efficiency / Nominals.NominalEfficiency);
            float vib  = 1f - Mathf.Clamp01(vibration / (Nominals.MaxVibration * 4f));
            float tmp  = 1f - Mathf.Clamp01((temp - Nominals.NominalTemperature * 0.95f) / (Nominals.NominalTemperature * 0.2f));
            return Mathf.Clamp((eff * 0.4f + vib * 0.35f + tmp * 0.25f) * 100f * (1f - deg * 0.3f), 0f, 100f);
        }

        private PumpStatus ComputeStatus(float temp, float vib, float inletP, float flow, float rpm, float health)
        {
            if (rpm < 10f) return PumpStatus.Offline;
            if (temp > Nominals.NominalTemperature * 1.15f || vib > Nominals.MaxVibration * 4f || inletP < Nominals.NominalInletPressure * 0.3f || health < 40f) return PumpStatus.Critical;
            if (temp > Nominals.NominalTemperature * 1.08f || vib > Nominals.MaxVibration * 2f  || inletP < Nominals.NominalInletPressure * 0.6f || health < 65f) return PumpStatus.Warning;
            return PumpStatus.Normal;
        }
    }
}
