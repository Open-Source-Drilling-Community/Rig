namespace NORCE.Drilling.Rig.Model
{
    public class CoilDriveSystem : RigEquipmentBase
    {
        public MountingType? CoilDrvClass { get; set; }
        public double? ReelPayloadCapacity { get; set; }
        public double? ReelPayloadLength { get; set; }
        public double? ReelRemainingLength { get; set; }
        public double? InjectorHeadRadius { get; set; }
        public double? InjectorHeadMinTubingOd { get; set; }
        public double? InjHeadDesignPullCapacity { get; set; }
        public double? InjHeadDesignSnubCapacity { get; set; }
        public double? InjHeadPullCapacity { get; set; }
        public double? InjHeadSnubCapacity { get; set; }
        public double? InjHeadMaxSpeed { get; set; }
        public double? CtLoad { get; set; }
        public double? CtWeightOnBit { get; set; }
        public double? CtCoilSpeed { get; set; }
        public double? CtCircPressure { get; set; }
        public double? CtWellheadPressure { get; set; }
        public double? CtEngineSpeed { get; set; }
        public double? CtInjHeadDrivePressure { get; set; }
        public double? CtInjTubingReelDrivePress { get; set; }
        public double? CtChainTension { get; set; }

        public CoilDriveSystem() { }
    }
}



