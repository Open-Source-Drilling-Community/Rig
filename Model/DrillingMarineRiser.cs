namespace NORCE.Drilling.Rig.Model
{
    public class DrillingMarineRiser : RigEquipmentBase
    {
        public RiserClass? RiserClass { get; set; }
        public double? JointWeight { get; set; }
        public double? RiserInsideDiameter { get; set; }
        public double? RiserOuterDiameter { get; set; }
        public double? RiserJointLength { get; set; }
        public double? RiserTotalLength { get; set; }
        public double? MaxLimitTensionLoad { get; set; }
        public double? MaxLimitOpTensionLoad { get; set; }
        public double? MaxLimitDesignKillPressure { get; set; }
        public double? MaxLimitOpKillPressure { get; set; }
        public double? MaxLimitDesignBoosterPressure { get; set; }
        public double? MaxLimitBoosterPressure { get; set; }
        public double? MaxLimitOpTemperature { get; set; }
        public double? MinLimitOpTemperature { get; set; }
        public double? MaxLimitAngleRiser { get; set; }

        public DrillingMarineRiser() { }
    }
}



