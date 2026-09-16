using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ThermalPumpDT.Core;
using ThermalPumpDT.Anomaly;

namespace ThermalPumpDT.UI
{
    /// <summary>
    /// Root HUD controller.  Binds the left pump-selector list, centre 3D camera
    /// and right sensor panel.  Requires Unity UI + TextMeshPro package.
    /// </summary>
    public class PumpDashboard : MonoBehaviour
    {
        [Header("Left Panel – Pump List")]
        public Transform PumpListContainer;
        public GameObject PumpListItemPrefab;

        [Header("Right Panel – Sensor Readouts")]
        public TextMeshProUGUI PumpNameLabel;
        public TextMeshProUGUI PumpStatusLabel;
        public GaugeWidget     RPMGauge;
        public GaugeWidget     FlowGauge;
        public GaugeWidget     InletPressureGauge;
        public GaugeWidget     OutletPressureGauge;
        public GaugeWidget     TemperatureGauge;
        public GaugeWidget     VibrationGauge;

        [Header("Health Panel")]
        public Image  HealthRingImage;       // filled circle type
        public TextMeshProUGUI HealthLabel;
        public TextMeshProUGUI RULLabel;
        public TextMeshProUGUI RunHoursLabel;

        [Header("Alert Panel")]
        public AlertPanel Alerts;

        [Header("Time-Series Graph")]
        public TimeSeriesGraph Graph;

        // ── Internal ────────────────────────────────────────────────
        private PumpTwin _selectedTwin;
        private readonly Dictionary<string, Button> _pumpButtons = new();

        // ─────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            TwinManager.OnAnyPumpStateUpdated += OnStateUpdate;
            TwinManager.OnAnomalyRaised       += OnAnomaly;
            TwinManager.OnPumpSelected         += OnPumpSelected;
        }

        private void OnDisable()
        {
            TwinManager.OnAnyPumpStateUpdated -= OnStateUpdate;
            TwinManager.OnAnomalyRaised       -= OnAnomaly;
            TwinManager.OnPumpSelected         -= OnPumpSelected;
        }

        private void Start()
        {
            BuildPumpList();
            if (TwinManager.Instance?.SelectedTwin is { } t)
                OnPumpSelected(t);
        }

        // ─────────────────────────────────────────────────────────
        //  Event handlers
        // ─────────────────────────────────────────────────────────

        private void OnPumpSelected(PumpTwin twin)
        {
            _selectedTwin = twin;
            UpdatePumpListHighlight(twin.PumpId);
        }

        private void OnStateUpdate(PumpTwin twin, PumpState state)
        {
            if (_selectedTwin == null || twin.PumpId != _selectedTwin.PumpId) return;
            RefreshPanels(twin, state);
        }

        private void OnAnomaly(AnomalyEvent evt) => Alerts?.ShowAlert(evt);

        // ─────────────────────────────────────────────────────────
        //  UI builders
        // ─────────────────────────────────────────────────────────

        private void BuildPumpList()
        {
            if (TwinManager.Instance == null || PumpListContainer == null) return;

            foreach (var twin in TwinManager.Instance.AllTwins)
            {
                var go  = Instantiate(PumpListItemPrefab, PumpListContainer);
                var btn = go.GetComponent<Button>();
                var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl) lbl.text = twin.PumpName;

                string id = twin.PumpId;
                btn?.onClick.AddListener(() => TwinManager.Instance.SelectPump(id));
                _pumpButtons[id] = btn;
            }
        }

        private void UpdatePumpListHighlight(string selectedId)
        {
            foreach (var kvp in _pumpButtons)
            {
                var colors = kvp.Value.colors;
                colors.normalColor = kvp.Key == selectedId
                    ? new Color(0.2f, 0.6f, 1f) : Color.white;
                kvp.Value.colors = colors;
            }
        }

        private void RefreshPanels(PumpTwin twin, PumpState state)
        {
            if (PumpNameLabel)   PumpNameLabel.text   = twin.PumpName;
            if (PumpStatusLabel)
            {
                PumpStatusLabel.text  = state.Status.ToString();
                PumpStatusLabel.color = StatusColor(state.Status);
            }

            RPMGauge?.UpdateValue(state.RPM,             twin.Nominals.NominalRPM,     twin.Nominals.NominalRPM * 1.2f);
            FlowGauge?.UpdateValue(state.FlowRate,        twin.Nominals.NominalFlow,    twin.Nominals.NominalFlow * 1.3f);
            InletPressureGauge?.UpdateValue(state.InletPressure,  twin.Nominals.NominalInletPressure,  twin.Nominals.NominalInletPressure * 1.6f);
            OutletPressureGauge?.UpdateValue(state.OutletPressure, twin.Nominals.NominalOutletPressure, twin.Nominals.NominalOutletPressure * 1.2f);
            TemperatureGauge?.UpdateValue(state.Temperature, twin.Nominals.NominalTemperature, twin.Nominals.NominalTemperature * 1.3f);
            VibrationGauge?.UpdateValue(state.VibrationRMS, twin.Nominals.MaxVibration, twin.Nominals.MaxVibration * 5f);

            float h = state.HealthScore / 100f;
            if (HealthRingImage) { HealthRingImage.fillAmount = h; HealthRingImage.color = HealthColor(state.HealthScore); }
            if (HealthLabel)   HealthLabel.text   = $"{state.HealthScore:F0}%";
            if (RunHoursLabel) RunHoursLabel.text = $"{state.RunHours:F1} h";

            var sched = FindObjectOfType<ThermalPumpDT.Anomaly.MaintenanceScheduler>();
            if (sched && RULLabel)
            {
                float rul = sched.GetRUL(twin.PumpId);
                RULLabel.text = rul > 0 ? $"RUL: {rul:F0} h" : "RUL: —";
            }

            Graph?.AddSample(state.RPM / twin.Nominals.NominalRPM,
                             state.Temperature / twin.Nominals.NominalTemperature,
                             state.VibrationRMS / (twin.Nominals.MaxVibration * 4f));
        }

        private static Color StatusColor(PumpStatus s) => s switch
        {
            PumpStatus.Normal   => new Color(0.2f, 0.8f, 0.3f),
            PumpStatus.Warning  => new Color(1f,   0.7f, 0f),
            PumpStatus.Critical => new Color(0.9f, 0.1f, 0.1f),
            _                   => Color.gray
        };

        private static Color HealthColor(float h) =>
            h > 70 ? new Color(0.2f, 0.8f, 0.3f) :
            h > 40 ? new Color(1f,   0.7f, 0f)   :
                     new Color(0.9f, 0.1f, 0.1f);
    }
}
