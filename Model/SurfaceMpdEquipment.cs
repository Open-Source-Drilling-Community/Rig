namespace NORCE.Drilling.Rig.Model
{
    public class SurfaceMpdEquipment : RigEquipmentBase
    {
        public SurfaceMpdClass? SurfaceMpdClass { get; set; }
        public double? MinimumBoreholeSize { get; set; }
        public double? MaximumBoreholeSize { get; set; }
        public double? PressureAccuracy { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitOperatingPressure { get; set; }
        public double? MinLimitOperatingPressure { get; set; }
        public double? MaxLimitFlowrate { get; set; }
        public double? MaxLimitMudWeight { get; set; }
        public double? MaxLimitPressure { get; set; }
        public double? MinLimitMudPumpFlowrate { get; set; }
        public double? StrokeRate { get; set; }
        public double? FlowRate { get; set; }
        public double? PressureAtDischarge { get; set; }
        public double? Power { get; set; }
        public double? PressureAtInlet { get; set; }

        public SurfaceMpdEquipment() { }
    }
}



