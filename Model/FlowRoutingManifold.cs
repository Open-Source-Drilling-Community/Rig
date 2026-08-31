using System.Collections.Generic;

namespace OSDC.Drilling.Rig.Model
{
    public class FlowRoutingManifold : RigEquipmentBase
    {
        public ManifoldClass? ManifoldType { get; set; }
        public double? FlangeSize { get; set; }
        public double? ReliefLineDiameter { get; set; }
        public double? EqualizationLineDiameter { get; set; }
        public double? PressureReliefValveTrim { get; set; }
        public ManifoldFlowPath? ManifoldFlowPath { get; set; }
        public List<RoutingManifoldCurvePoint>? ManifoldFlowcurves { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitOperatingPressure { get; set; }
        public double? MaxLimitOperatingTemperature { get; set; }
        public double? MinLimitOperatingTemperature { get; set; }
        public double? MaxLimitFlowrate { get; set; }
        public FlowRoutingManifold() { }
    }
}



