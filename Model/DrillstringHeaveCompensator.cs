namespace NORCE.Drilling.Rig.Model
{
    public class DrillstringHeaveCompensator : RigEquipmentBase
    {
        public HeaveCompensatorClass? HeaveCompClass { get; set; }
        public double? CompensatorCapacity { get; set; }
        public CompensatorStatus? CompensatorStatus { get; set; }
        public double? MaxLimitCompensatorStroke { get; set; }

        public DrillstringHeaveCompensator() { }
    }
}



