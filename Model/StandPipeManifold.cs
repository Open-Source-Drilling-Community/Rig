namespace NORCE.Drilling.Rig.Model
{
    public class StandPipeManifold : RigEquipmentBase
    {
        public double? PipeDiameter { get; set; }
        public StandpipeSpecLevel? StandpipeSpecLevel { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitOperatingPressure { get; set; }
        public double? MaxLimitOperatingTemperature { get; set; }
        public double? MinLimitOperatingTemperature { get; set; }

        public StandPipeManifold() { }
    }
}
