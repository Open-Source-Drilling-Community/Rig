namespace NORCE.Drilling.Rig.Model
{
    public class AutoDriller : RigEquipmentBase
    {
        public AutodrillerControlMode? ControlMode { get; set; }
        public double? MaxLimitRop { get; set; }
        public double? MinLimitRop { get; set; }
        public double? MaxLimitWob { get; set; }
        public double? MinLimitWob { get; set; }
        public double? MaxLimitDifferentialPressure { get; set; }
        public double? MinLimitDifferentialPressure { get; set; }
        public double? MaxLimitTrq { get; set; }
        public double? MinLimitTrq { get; set; }
        public double? SetpointWob { get; set; }
        public double? SetpointRop { get; set; }
        public double? SetpointDiffp { get; set; }
        public double? SetpointTrq { get; set; }

        public AutoDriller() { }
    }
}



