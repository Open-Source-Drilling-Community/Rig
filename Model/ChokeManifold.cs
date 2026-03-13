namespace NORCE.Drilling.Rig.Model
{
    public class ChokeManifold : RigEquipmentBase
    {
        public ControlClass? ChokeControlClass { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitOperatingPressure { get; set; }
        public double? MinLimitOperatingPressure { get; set; }
        public double? MaxLimitTestPressure { get; set; }
        public double? MaxLimitOperatingTemperature { get; set; }
        public double? MinLimitOperatingTemperature { get; set; }

        public ChokeManifold() { }
    }
}
