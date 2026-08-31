namespace OSDC.Drilling.Rig.Model
{
    public class CrownBlock : RigEquipmentBase
    {
        public double? SheaveDiameter { get; set; }
        public double? GrooveDiameter { get; set; }
        public int? NumberOfSheaves { get; set; }
        public double? MaxLimitDesignLoad { get; set; }
        public double? MaxLimitOperatingLoad { get; set; }
        public double? MaxLimitCompensatorStroke { get; set; }
        public CrownBlock() { }
    }
}



