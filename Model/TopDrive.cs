namespace NORCE.Drilling.Rig.Model
{
    public class TopDrive : RigEquipmentBase
    {
        public TopDriveClass? TopDriveClass { get; set; }
        public TopDriveControllerType? TopDriveControllerType { get; set; }
        public bool? Orientable { get; set; }
        public double? Weight { get; set; }
        public double? MaxLimitIbopPressure { get; set; }
        public double? MaxLimitRotationSpeed { get; set; }
        public double? MaxLimitDesignLoad { get; set; }
        public double? MaxLimitDesignTorque { get; set; }
        public double? MaxLimitOperatingLoad { get; set; }
        public double? MaxLimitOperatingTorque { get; set; }
        public double? MaxLimitMakeupTorque { get; set; }
        public double? MaxLimitBreakoutTorque { get; set; }
        public double? TopDriveHeight { get; set; }
        public double? ProportionalGain { get; set; } = null;
        public double? IntegralGain { get; set; } = null;
        public double? TuningFrequency { get; set; } = null;
        public double? VFDFilterTimeConstant { get; set; } = null;
        public double? EncoderTimeConstant { get; set; } = null;
        public double? AccelerationFilterTimeConstant { get; set; } = null;
        public double? TorqueHighPassFilterTimeConstant { get; set; } = null;
        public double? TorqueLowPassFilterTimeConstant { get; set; } = null;
        public double? TuningFactor { get; set; } = null;
        public double? InertiaCorrectionFactor { get; set; } = null;

        public TopDrive() { }
    }
}
