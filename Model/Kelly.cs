namespace OSDC.Drilling.Rig.Model
{
    public class Kelly : RigEquipmentBase
    {
        public KellyClass? KellyClass { get; set; }
        public double? KellyJointLength { get; set; }
        public double? MaxLimitDesignRotationSpeed { get; set; }
        public double? MaxLimitDesignTorque { get; set; }
        public double? MaxLimitIbopPressure { get; set; }
        public double? MaxLimitRotationSpeed { get; set; }
        public double? MaxLimitTorque { get; set; }
        public Kelly() { }
    }
}
