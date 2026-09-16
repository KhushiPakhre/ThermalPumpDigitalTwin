using System;
using System.Collections.Generic;
using UnityEngine;
using ThermalPumpDT.Core;

namespace ThermalPumpDT.Anomaly
{
    /// <summary>
    /// Computes Remaining Useful Life (RUL) and schedules maintenance windows
    /// based on each pump twin health score trajectory.
    /// </summary>
    public class MaintenanceScheduler : MonoBehaviour
    {
        [Tooltip("Maintenance is recommended when health drops below this threshold.")]
        public float MaintenanceThreshold = 70f;

        // Per-pump sliding window of health scores for trend analysis
        private readonly Dictionary<string, Queue<float>> _healthHistory = new();
        private readonly Dictionary<string, float>        _rul           = new();

        private const int WINDOW = 60; // samples

        private void OnEnable()  => TwinManager.OnAnyPumpStateUpdated += OnState;
        private void OnDisable() => TwinManager.OnAnyPumpStateUpdated -= OnState;

        private void OnState(PumpTwin twin, PumpState state)
        {
            string id = twin.PumpId;
            if (!_healthHistory.ContainsKey(id))
                _healthHistory[id] = new Queue<float>();

            var q = _healthHistory[id];
            q.Enqueue(state.HealthScore);
            if (q.Count > WINDOW) q.Dequeue();

            if (q.Count >= 5)
                _rul[id] = EstimateRUL(q, state.HealthScore);
        }

        /// <summary>Returns estimated hours until health < MaintenanceThreshold. -1 if unknown.</summary>
        public float GetRUL(string pumpId) =>
            _rul.TryGetValue(pumpId, out float v) ? v : -1f;

        public DateTime GetNextMaintenanceDate(string pumpId)
        {
            float rul = GetRUL(pumpId);
            return rul > 0 ? DateTime.Now.AddHours(rul) : DateTime.Now.AddDays(30);
        }

        private float EstimateRUL(Queue<float> history, float currentHealth)
        {
            // Linear regression on the health series
            var arr = history.ToArray();
            int n = arr.Length;
            float sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            for (int i = 0; i < n; i++)
            {
                sumX  += i; sumY  += arr[i];
                sumXY += i * arr[i]; sumX2 += i * i;
            }
            float denom = n * sumX2 - sumX * sumX;
            if (Mathf.Abs(denom) < 0.001f) return 9999f;

            float slope = (n * sumXY - sumX * sumY) / denom; // health/sample
            if (slope >= 0f) return 9999f; // not degrading

            // samples to reach threshold
            float samplesToThreshold = (currentHealth - MaintenanceThreshold) / (-slope);
            // convert samples → hours (1 sample / second simulation → 1s = 1/3600 h)
            return samplesToThreshold / 3600f;
        }
    }
}
