namespace NORCE.Drilling.Rig.Model
{
    public class MarineMpdEquipment : RigEquipmentBase
    {
        public MarineMpdClass? MarineMpdClass { get; set; }
        public double? Length { get; set; }
        public double? Weight { get; set; }
        public double? ThroughBoreDiameter { get; set; }
        public ControllerType? ControlMeans { get; set; }
        public bool? ContainsFlowSpool { get; set; }
        public bool? ContainsNonRotatingDevice { get; set; }
        public bool? ContainsDrillstringIsolation { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitDynamicPressure { get; set; }
        public double? MaxLimitRotatingSpeed { get; set; }

        public MarineMpdEquipment() { }
    }
}



