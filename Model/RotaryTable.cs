namespace NORCE.Drilling.Rig.Model
{
    public class RotaryTable : RigEquipmentBase
    {
        public RotaryTableType? RotaryTableType { get; set; }
        public double? TableOpeningDiameter { get; set; }
        public RotaryTableBushingType? BushingType { get; set; }
        public double? BushingSize { get; set; }
        public double? Height { get; set; }
        public double? Mass { get; set; }
        public double? MaxLimitOperatingSpeed { get; set; }
        public double? MaxLimitDesignSpeed { get; set; }
        public double? MaxLimitOperatingTorque { get; set; }
        public double? MaxLimitDesignTorque { get; set; }
        public double? MaxLimitOperatingStringWeight { get; set; }
        public double? MaxLimitDesignStringWeight { get; set; }
        public double? MaxLimitPower { get; set; }
        public double? MaxLimitTemperature { get; set; }

        public RotaryTable() { }
    }
}
