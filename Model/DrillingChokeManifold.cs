using System.Collections.Generic;

namespace NORCE.Drilling.Rig.Model
{
    public class DrillingChokeManifold : RigEquipmentBase
    {
        public ManifoldClass? ManifoldType { get; set; }
        public double? TrimSize { get; set; }
        public string? FlowMeter { get; set; }
        public double? FlowMeterSize { get; set; }
        public double? FlowMeterPressureRating { get; set; }
        public bool? JunkBasket { get; set; }
        public string? ChokeCount { get; set; }
        public string? FlowMeterCount { get; set; }
        public string? PressureSensorVotingNumber { get; set; }
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
        public double? PressureBeforeChoke { get; set; }
        public double? PressureAfterChoke { get; set; }
        public double? CvValue { get; set; }
        public double? CloggingOccuring { get; set; }
        public double? TemperatureBeforeChoke { get; set; }
        public double? TemperatureAfterChoke { get; set; }
        public double? FlowThroughChoke { get; set; }
        public double? MudDensityOut { get; set; }
        public double? MudDensityIn { get; set; }
        public double? ReliefValvePressure { get; set; }
        public double? PressureBeforeFlowMeter { get; set; }
        public double? PressureAfterFlowMeter { get; set; }
        public double? InletPressure { get; set; }
        public double? OutletPressure { get; set; }
        public int? VotingSensorsFailed { get; set; }

        public DrillingChokeManifold() { }
    }
}



