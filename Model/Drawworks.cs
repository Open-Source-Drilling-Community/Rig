namespace NORCE.Drilling.Rig.Model
{
    public class Drawworks : RigEquipmentBase
    {
        public DrawworksClass? DrawworksClass { get; set; }
        public double? MaxLimitDesignLoad { get; set; }
        public double? MaxLimitOperatingLoad { get; set; }
        public double? MaxLimitContinuousDrumPower { get; set; }
        public double? MaxLimitContinuousDrumTorque { get; set; }

        public Drawworks() { }
    }
}



