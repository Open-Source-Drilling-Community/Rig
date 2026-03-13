namespace NORCE.Drilling.Rig.Model
{
    public class Derrick : RigEquipmentBase
    {
        public DerrickClass? DerrickClass { get; set; }
        public double? Height { get; set; }
        public int? MaxLimitJointsPerStand { get; set; }
        public double? MaxLimitDesignLoad { get; set; }
        public double? MaxLimitOperatingLoad { get; set; }
        public double? MaxLimitWindSpeed { get; set; }

        public Derrick() { }
    }
}



