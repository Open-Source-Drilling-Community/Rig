namespace NORCE.Drilling.Rig.Model
{
    public class FloatValve : RigEquipmentBase
    {
        public FloatValveClass? FloatValveClass { get; set; }
        public double? Diameter { get; set; }
        public double? Length { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitOperatingPressure { get; set; }

        public FloatValve() { }
    }
}



