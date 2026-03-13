namespace NORCE.Drilling.Rig.Model
{
    public class MpdControlDevice : RigEquipmentBase
    {
        public MpdControlDeviceClass? MpdControlDeviceClass { get; set; }
        public double? NominalSize { get; set; }
        public double? ThroughBoreDiameter { get; set; }
        public string? SealingElementMaterial { get; set; }
        public double? ControlDeviceHeight { get; set; }
        public double? MaxLimitStaticPressure { get; set; }
        public double? MaxLimitDynamicPressure { get; set; }
        public double? MaxLimitRotatingSpeed { get; set; }
        public double? MaxLimitActivationPressure { get; set; }

        public MpdControlDevice() { }
    }
}



