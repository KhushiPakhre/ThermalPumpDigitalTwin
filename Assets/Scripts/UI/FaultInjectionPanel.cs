using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ThermalPumpDT.Core;

namespace ThermalPumpDT.UI
{
    /// <summary>
    /// Debug/Demo panel to inject and clear faults on the selected pump at runtime.
    /// Bind via Inspector to buttons in the scene.
    /// </summary>
    public class FaultInjectionPanel : MonoBehaviour
    {
        [Header("Buttons – assign in Inspector")]
        public Button CavitationBtn;
        public Button BearingWearBtn;
        public Button OverheatBtn;
        public Button SealLeakBtn;
        public Button ImpellerWearBtn;
        public Button DeadHeadBtn;
        public Button ClearAllBtn;
        public Button MaintenanceBtn;

        [Header("Status")]
        public TextMeshProUGUI ActiveFaultsLabel;

        private void Start() => RegisterButtons();

        private void RegisterButtons()
        {
            Bind(CavitationBtn,   () => Inject(FaultType.Cavitation));
            Bind(BearingWearBtn,  () => Inject(FaultType.BearingWear));
            Bind(OverheatBtn,     () => Inject(FaultType.Overheating));
            Bind(SealLeakBtn,     () => Inject(FaultType.SealLeak));
            Bind(ImpellerWearBtn, () => Inject(FaultType.ImpellerWear));
            Bind(DeadHeadBtn,     () => Inject(FaultType.DeadHead));
            Bind(ClearAllBtn,     ClearAll);
            Bind(MaintenanceBtn,  DoMaintenance);
        }

        private static void Bind(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null) btn.onClick.AddListener(action);
        }

        private void Inject(FaultType fault)
        {
            var sim = GetSelectedSim();
            sim?.InjectFault(fault);
            UpdateLabel();
        }

        private void ClearAll()
        {
            GetSelectedSim()?.ClearAllFaults();
            UpdateLabel();
        }

        private void DoMaintenance()
        {
            var twin = TwinManager.Instance?.SelectedTwin;
            twin?.PerformMaintenance();
            ClearAll();
        }

        private void UpdateLabel()
        {
            if (ActiveFaultsLabel == null) return;
            var sim = GetSelectedSim();
            if (sim == null) { ActiveFaultsLabel.text = "No pump selected"; return; }
            string faults = string.Join(", ", sim.ActiveFaults);
            ActiveFaultsLabel.text = string.IsNullOrEmpty(faults) ? "No active faults" : $"Active: {faults}";
        }

        private PumpSimulator GetSelectedSim()
        {
            var twin = TwinManager.Instance?.SelectedTwin;
            return twin != null ? TwinManager.Instance.GetSimulator(twin.PumpId) : null;
        }
    }
}
