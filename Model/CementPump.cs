using System.Collections.Generic;

namespace NORCE.Drilling.Rig.Model
{
    public class CementPump : RigEquipmentBase
    {
        public PumpClass? PumpClass { get; set; }
        public double? PlungerDiameter { get; set; }
        public double? StrokeLength { get; set; }
        public List<CementPumpDisplacementPoint>? CementPumpDisplacement { get; set; }
        public double? MaxLimitPressure { get; set; }
        public double? MaxLimitFlowRate { get; set; }

        public CementPump() { }
    }
}



