namespace NORCE.Drilling.Rig.Model
{
    public class TravellingBlock : RigEquipmentBase
    {
        public double? Weight { get; set; }
        public int? NumberOfSheaves { get; set; }
        public double? GrooveDiameter { get; set; }
        public double? MaxLimitBlockTravel { get; set; }
        public double? MaxLimitDesignLoad { get; set; }
        public double? MaxLimitOperatingLoad { get; set; }
        public double? HookVelocity { get; set; }
        public double? HookPosition { get; set; }

        public TravellingBlock() { }
    }
}



