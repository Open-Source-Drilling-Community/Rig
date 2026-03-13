namespace NORCE.Drilling.Rig.Model
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
        public double? SurfaceRotation { get; set; }
        public double? SurfaceTorque { get; set; }
        public double? KellyHeight { get; set; }

        public Kelly() { }
    }
}
