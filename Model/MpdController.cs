namespace NORCE.Drilling.Rig.Model
{
    public class MpdController : RigEquipmentBase
    {
        public MpdGradientMode? MpdGradientMode { get; set; }
        public double? PrimaryChokeTrim { get; set; }
        public double? SecondaryChokeTrim { get; set; }
        public double? MaxLimitPressure { get; set; }
        public double? MinLimitMudPumpFlowrate { get; set; }
        public double? ManipulatedMpdChoke { get; set; }
        public double? ManipulatedLiftPumpRate { get; set; }
        public double? ControlledDownholePressure { get; set; }
        public double? BackpressureFlowrate { get; set; }
        public double? AnnulusRefillFlowrate { get; set; }

        public MpdController() { }
    }
}



