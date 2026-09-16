using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThermalPumpDT.Core
{
    /// <summary>
    /// Singleton MonoBehaviour that owns all PumpTwin instances and their simulators.
    /// Provides a central bus for state-change events across the application.
    /// </summary>
    public class TwinManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────
        public static TwinManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────
        /// Fired whenever any pump state changes.
        public static event Action<PumpTwin, PumpState> OnAnyPumpStateUpdated;
        /// Fired whenever any anomaly is detected.
        public static event Action<AnomalyEvent> OnAnomalyRaised;

        // ── Pumps ─────────────────────────────────────────────────
        private readonly Dictionary<string, PumpTwin>      _twins      = new();
        private readonly Dictionary<string, PumpSimulator> _simulators = new();

        public IEnumerable<PumpTwin> AllTwins => _twins.Values;

        // ── Selected pump ─────────────────────────────────────────
        public PumpTwin SelectedTwin { get; private set; }
        public static event Action<PumpTwin> OnPumpSelected;

        // ─────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateDefaultPumps();
        }

        // ─────────────────────────────────────────────────────────
        //  Initialisation
        // ─────────────────────────────────────────────────────────

        private void CreateDefaultPumps()
        {
            // Boiler Feed Water Pumps
            RegisterPump("BFP-01", "Boiler Feed Pump A", PumpType.BoilerFeedWater,  0f);
            RegisterPump("BFP-02", "Boiler Feed Pump B", PumpType.BoilerFeedWater, 13f);

            // Condenser Extraction Pump
            RegisterPump("CEP-01", "Condenser Extraction Pump", PumpType.CondenserExtraction, 27f);

            SelectedTwin = _twins["BFP-01"];
        }

        private void RegisterPump(string id, string name, PumpType type, float noiseSeed)
        {
            var twin = new PumpTwin(id, name, type);
            twin.OnStateUpdated += state => OnAnyPumpStateUpdated?.Invoke(twin, state);

            var simGO  = new GameObject($"Simulator_{id}");
            simGO.transform.SetParent(transform);
            var sim = simGO.AddComponent<PumpSimulator>();
            sim.PumpId          = id;
            sim.NoiseSeedOffset = noiseSeed;
            sim.Initialize(twin);

            _twins[id]      = twin;
            _simulators[id] = sim;
        }

        // ─────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────

        public PumpTwin GetTwin(string id) =>
            _twins.TryGetValue(id, out var t) ? t : null;

        public PumpSimulator GetSimulator(string id) =>
            _simulators.TryGetValue(id, out var s) ? s : null;

        public void SelectPump(string id)
        {
            if (_twins.TryGetValue(id, out var t))
            {
                SelectedTwin = t;
                OnPumpSelected?.Invoke(t);
            }
        }

        public void RaiseAnomaly(AnomalyEvent evt) => OnAnomalyRaised?.Invoke(evt);

        public void LoadDetailScene(string pumpId)
        {
            SelectPump(pumpId);
            SceneManager.LoadScene("PumpDetail");
        }

        public void LoadPlantScene() => SceneManager.LoadScene("MainPlant");
    }
}
