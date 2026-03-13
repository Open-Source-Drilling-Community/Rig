using System.Collections.Generic;

namespace NORCE.Drilling.Rig.Model
{
    public class BopStack : RigEquipmentBase
    {
        public BopStackClass? BopStackClass { get; set; }
        public string? UnitReferenceList { get; set; }
        public ControllerType? BopControlType { get; set; }
        public double? BoreDiameter { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public List<BopStackComponentDefinition>? BopComponents { get; set; }
        public List<BopLineDefinition>? BopLines { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitOperatingPressure { get; set; }
        public double? MinLimitOperatingPressure { get; set; }
        public double? BopLineMaxLimitDesignPressure { get; set; }
        public double? BopLineMaxLimitOperatingPressure { get; set; }
        public double? CasingPressure { get; set; }
        public double? KillLinePressure { get; set; }
        public double? ChokeLinePressure { get; set; }
        public double? ShutInDrillpipePressure { get; set; }

        public BopStack() { }
    }
}



