namespace NORCE.Drilling.Rig.Model
{
    public class Accumulator : RigEquipmentBase
    {
        public AccumulatorClass? AccumulatorClass { get; set; }
        public double? Capacity { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitOperatingPressure { get; set; }

        public Accumulator() { }
    }
}



