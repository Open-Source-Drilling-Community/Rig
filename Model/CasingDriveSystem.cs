namespace NORCE.Drilling.Rig.Model
{
    public class CasingDriveSystem : RigEquipmentBase
    {
        public CasingDriveClass? CsgDrvClass { get; set; }
        public double? HoistingCapacity { get; set; }
        public double? Length { get; set; }
        public double? MaxLimitDesignTorque { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitDesignRotationSpeed { get; set; }
        public double? MaxLimitTorque { get; set; }
        public double? MaxLimitPressure { get; set; }
        public double? MaxLimitRotationSpeed { get; set; }
        public double? MaxLimitPushDown { get; set; }

        public CasingDriveSystem() { }
    }
}



