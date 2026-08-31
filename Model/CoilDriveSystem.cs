namespace OSDC.Drilling.Rig.Model
{
    public class CoilDriveSystem : RigEquipmentBase
    {
        public MountingType? CoilDrvClass { get; set; }
        public double? ReelPayloadCapacity { get; set; }
        public double? ReelPayloadLength { get; set; }
        public double? InjectorHeadRadius { get; set; }
        public double? InjectorHeadMinTubingOd { get; set; }
        public double? InjHeadDesignPullCapacity { get; set; }
        public double? InjHeadDesignSnubCapacity { get; set; }
        public double? InjHeadPullCapacity { get; set; }
        public double? InjHeadSnubCapacity { get; set; }
        public double? InjHeadMaxSpeed { get; set; }
        public CoilDriveSystem() { }
    }
}



