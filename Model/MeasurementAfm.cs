using System.Collections.Generic;

namespace NORCE.Drilling.Rig.Model
{
    public class MeasurementAfm : RigEquipmentBase
    {
        public double? UpdateRate { get; set; }
        public bool? Active { get; set; }
        public double? AfmMudDensity { get; set; }
        public double? AfmMudTemperature { get; set; }
        public double? AfmPv { get; set; }
        public double? AfmYp { get; set; }
        public List<RheometerAfmMeasurement>? AfmRheometerMeasurements { get; set; }
        public double? RtViscConsistencyIndex { get; set; }
        public double? RtViscFlowBehaviorIndex { get; set; }

        public MeasurementAfm() { }
    }
}



