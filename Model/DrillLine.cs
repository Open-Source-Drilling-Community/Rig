namespace NORCE.Drilling.Rig.Model
{
    public class DrillLine : RigEquipmentBase
    {
        public int? Number { get; set; }
        public double? Diameter { get; set; }
        public double? LinearWeight { get; set; }
        public double? MaxLimitDesignBreakingLoad { get; set; }
        public double? MaxLimitOperatingBreakingLoad { get; set; }
        public double? Hookload { get; set; }

        public DrillLine() { }
    }
}



