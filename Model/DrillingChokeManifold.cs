using System.Collections.Generic;

namespace OSDC.Drilling.Rig.Model
{
    public class DrillingChokeManifold : RigEquipmentBase
    {
        public ManifoldClass? ManifoldType { get; set; }
        public double? TrimSize { get; set; }
        public string? FlowMeter { get; set; }
        public double? FlowMeterSize { get; set; }
        public double? FlowMeterPressureRating { get; set; }
        public bool? JunkBasket { get; set; }
        public int? ChokeCount { get; set; }
        public int? FlowMeterCount { get; set; }
        public int? PressureSensorVotingNumber { get; set; }
        public ChokeNumber? ChokeNumber { get; set; }
        public ChokeFunction? ChokeFunction { get; set; }
        public List<ChokeCvCurvePoint>? ChokeCvCurves { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitOperatingPressure { get; set; }
        public double? MaxLimitOperatingTemperature { get; set; }
        public double? MinLimitOperatingTemperature { get; set; }
        public double? MaxLimitOpeningSpeed { get; set; }
        public double? MaxLimitBackPressure { get; set; }
        public double? MinLimitFlowrate { get; set; }
        public double? MaxLimitFlowrate { get; set; }
        public DrillingChokeManifold() { }
    }
}



