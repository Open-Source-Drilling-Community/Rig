namespace NORCE.Drilling.Rig.Model
{
    public class RiserHeaveCompensator : RigEquipmentBase
    {
        public RiserCompensatorClass? RiserCompensatorClass { get; set; }
        public double? CompensatorCapacity { get; set; }
        public CompensatorStatus? CompensatorStatus { get; set; }
        public double? MaxLimitCompensatorStroke { get; set; }

        public RiserHeaveCompensator() { }
    }
}



