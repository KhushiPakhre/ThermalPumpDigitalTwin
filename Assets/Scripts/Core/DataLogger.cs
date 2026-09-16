using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using ThermalPumpDT.Core;

namespace ThermalPumpDT.Core
{
    /// <summary>
    /// Subscribes to TwinManager events and persists pump readings
    /// to JSON (line-delimited) and optionally exports CSV.
    /// </summary>
    public class DataLogger : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Maximum log entries kept in memory before flushing to disk.")]
        public int FlushEveryNEntries = 50;
        public bool EnableLogging = true;

        private readonly List<PumpStateRecord> _buffer = new();
        private string _logPath;

        [Serializable]
        private class PumpStateRecord
        {
            public string PumpId;
            public string PumpName;
            public PumpState State;
        }

        private void OnEnable()
        {
            _logPath = Path.Combine(Application.streamingAssetsPath, "logs", "pump_log.jsonl");
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            TwinManager.OnAnyPumpStateUpdated += HandleStateUpdate;
        }

        private void OnDisable() => TwinManager.OnAnyPumpStateUpdated -= HandleStateUpdate;

        private void HandleStateUpdate(PumpTwin twin, PumpState state)
        {
            if (!EnableLogging) return;
            _buffer.Add(new PumpStateRecord { PumpId = twin.PumpId, PumpName = twin.PumpName, State = state });
            if (_buffer.Count >= FlushEveryNEntries) Flush();
        }

        private void OnApplicationQuit() => Flush();

        public void Flush()
        {
            if (_buffer.Count == 0) return;
            using var sw = new StreamWriter(_logPath, append: true, Encoding.UTF8);
            foreach (var record in _buffer)
                sw.WriteLine(JsonUtility.ToJson(record));
            _buffer.Clear();
        }

        /// <summary>Export all logged data as CSV to StreamingAssets/logs/pump_export.csv</summary>
        public void ExportCSV()
        {
            Flush();
            string csvPath = Path.Combine(Application.streamingAssetsPath, "logs", "pump_export.csv");
            if (!File.Exists(_logPath)) { Debug.LogWarning("No log file found."); return; }

            var sb = new StringBuilder();
            sb.AppendLine("PumpId,PumpName,Timestamp,RPM,FlowRate,InletPressure,OutletPressure,Temperature,VibrationRMS,Efficiency,HealthScore,RunHours,Status");

            foreach (var line in File.ReadAllLines(_logPath))
            {
                try
                {
                    var r = JsonUtility.FromJson<PumpStateRecord>(line);
                    var s = r.State;
                    sb.AppendLine($"{r.PumpId},{r.PumpName},{s.Timestamp},{s.RPM:F1},{s.FlowRate:F1},{s.InletPressure:F2},{s.OutletPressure:F2},{s.Temperature:F1},{s.VibrationRMS:F3},{s.EfficiencyPercent:F1},{s.HealthScore:F1},{s.RunHours:F2},{s.Status}");
                }
                catch { /* Skip malformed line */ }
            }

            File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[DataLogger] CSV exported to {csvPath}");
        }
    }
}
