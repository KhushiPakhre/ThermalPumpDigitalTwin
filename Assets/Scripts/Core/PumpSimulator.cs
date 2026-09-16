using System.Collections.Generic;
using UnityEngine;
using ThermalPumpDT.Anomaly;

namespace ThermalPumpDT.Core
{
    /// <summary>Drives a PumpTwin with Perlin-noise-based sensor data and fault injection.</summary>
    public class PumpSimulator : MonoBehaviour
    {
        [Header("References")]
        public string PumpId;
        [Header("Simulation")]
        [Range(0.1f,5f)] public float UpdateIntervalSeconds = 1f;
        public float NoiseSeedOffset = 0f;
        public float NoiseSpeed = 0.05f;

        private readonly HashSet<FaultType> _activeFaults = new HashSet<FaultType>();
        private PumpTwin   _twin;
        private PumpNominals _nom;
        private float _elapsed, _runHours, _noiseTime;

        public void Initialize(PumpTwin twin)
        {
            _twin = twin; _nom = twin.Nominals; _noiseTime = NoiseSeedOffset;
        }

        private void Update()
        {
            if (_twin == null) return;
            _elapsed   += Time.deltaTime;
            _noiseTime += Time.deltaTime * NoiseSpeed;
            _runHours  += Time.deltaTime / 3600f;
            if (_elapsed >= UpdateIntervalSeconds) { _elapsed = 0f; SimulateAndPush(); }
        }

        public void InjectFault(FaultType f)  { _activeFaults.Add(f);    Debug.Log($"[Sim] Fault injected: {f}"); }
        public void ClearFault(FaultType f)   { _activeFaults.Remove(f); Debug.Log($"[Sim] Fault cleared: {f}"); }
        public void ClearAllFaults()          { _activeFaults.Clear();   Debug.Log("[Sim] All faults cleared"); }
        public bool IsFaultActive(FaultType f) => _activeFaults.Contains(f);
        public IEnumerable<FaultType> ActiveFaults => _activeFaults;

        private void SimulateAndPush()
        {
            float t = _noiseTime;
            float rpm    = _nom.NominalRPM    * Noise(t, 0f,  0.02f);
            float flow   = _nom.NominalFlow   * Noise(t, 5f,  0.04f);
            float inletP = _nom.NominalInletPressure  * Noise(t, 10f, 0.03f);
            float outP   = _nom.NominalOutletPressure * Noise(t, 15f, 0.02f);
            float temp   = _nom.NominalTemperature    * Noise(t, 20f, 0.025f);
            float vib    = _nom.MaxVibration * Noise(t, 25f, 0.08f, 0.3f, 1.1f);
            float deg    = 0f;

            foreach (var fault in _activeFaults)
            {
                switch (fault)
                {
                    case FaultType.Cavitation:
                        inletP *= 0.35f + Mathf.Sin(t*8f)*0.1f; vib *= 3.5f + Mathf.Abs(Mathf.Sin(t*12f))*2f;
                        flow *= 0.75f; deg += 0.0002f; break;
                    case FaultType.BearingWear:
                        vib *= 2.8f + Mathf.PerlinNoise(t*0.5f, 1f)*2f; rpm *= 0.97f; deg += 0.00015f; break;
                    case FaultType.Overheating:
                        temp *= 1.2f + Mathf.Clamp01(t - NoiseSeedOffset)*0.05f; vib *= 1.3f; deg += 0.0001f; break;
                    case FaultType.SealLeak:
                        flow *= 0.6f + Mathf.PerlinNoise(t, 3f)*0.15f; outP *= 0.85f; deg += 0.00008f; break;
                    case FaultType.ImpellerWear:
                        flow *= 0.80f; outP *= 0.88f; vib *= 1.5f; deg += 0.00012f; break;
                    case FaultType.DeadHead:
                        flow = 0f; temp *= 1.18f; outP *= 1.3f; deg += 0.0003f; break;
                }
            }
            _twin.UpdateState(rpm, flow, inletP, outP, temp, vib, _runHours, deg);
        }

        private float Noise(float t, float off, float speed, float minM=0.95f, float maxM=1.05f)
            => Mathf.Lerp(minM, maxM, Mathf.PerlinNoise((t + off) * speed, NoiseSeedOffset));
    }
}
