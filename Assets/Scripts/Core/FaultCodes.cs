namespace ThermalPumpDT.Core
{
    public enum FaultType
    {
        None, Cavitation, BearingWear, Overheating, SealLeak, ImpellerWear, DeadHead
    }

    public enum FaultSeverity { Info, Warning, Critical }

    [System.Serializable]
    public class AnomalyEvent
    {
        public string PumpId;
        public FaultType Fault;
        public FaultSeverity Severity;
        public string Description;
        public string Timestamp;
        public bool Acknowledged;

        public AnomalyEvent(string pumpId, FaultType fault, FaultSeverity severity, string desc)
        {
            PumpId = pumpId; Fault = fault; Severity = severity;
            Description = desc; Timestamp = System.DateTime.UtcNow.ToString("o");
        }
    }
}
