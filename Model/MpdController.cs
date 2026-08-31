namespace OSDC.Drilling.Rig.Model
{
    public class MpdController : RigEquipmentBase
    {
        public MpdGradientMode? MpdGradientMode { get; set; }
        public double? PrimaryChokeTrim { get; set; }
        public double? SecondaryChokeTrim { get; set; }
        public double? MaxLimitPressure { get; set; }
        public double? MinLimitMudPumpFlowrate { get; set; }
        public MpdController() { }
    }
}



