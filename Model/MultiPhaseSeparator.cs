namespace NORCE.Drilling.Rig.Model
{
    public class MultiPhaseSeparator : RigEquipmentBase
    {
        public SeparatorPhaseClass? SeparatorClass { get; set; }
        public double? MaximumOperatingPressure { get; set; }
        public double? MaximumOperatingFlowrate { get; set; }
        public double? SeparationEfficiency { get; set; }
        public SeparatorMedium? SeparatorMedium { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitOperatingPressure { get; set; }
        public double? MaxLimitFlowrate { get; set; }
        public double? MaxLimitOperatingTemperature { get; set; }
        public double? MinLimitOperatingTemperature { get; set; }
        public double? PressureSeparator { get; set; }
        public double? TemperatureSeparator { get; set; }

        public MultiPhaseSeparator() { }
    }
}



