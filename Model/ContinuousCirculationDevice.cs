namespace NORCE.Drilling.Rig.Model
{
    public class ContinuousCirculationDevice : RigEquipmentBase
    {
        public ControlClass? CcdControlClass { get; set; }
        public double? WorkingPumpPressure { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitOperatingPressure { get; set; }
        public double? MaxLimitFlowrate { get; set; }
        public double? MaxLimitBackflow { get; set; }
        public double? MaxLimitFluidTemperature { get; set; }
        public double? MinLimitFluidTemperature { get; set; }
        public double? MaxLimitMudWeight { get; set; }
        public double? MaxLimitRotationRate { get; set; }

        public ContinuousCirculationDevice() { }
    }
}



