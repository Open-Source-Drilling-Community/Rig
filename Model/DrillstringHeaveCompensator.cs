namespace OSDC.Drilling.Rig.Model
{
    public class DrillstringHeaveCompensator : RigEquipmentBase
    {
        public HeaveCompensatorClass? HeaveCompClass { get; set; }
        public double? CompensatorCapacity { get; set; }
        public double? MaxLimitCompensatorStroke { get; set; }

        public DrillstringHeaveCompensator() { }
    }
}



