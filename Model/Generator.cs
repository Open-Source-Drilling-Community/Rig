namespace NORCE.Drilling.Rig.Model
{
    public class Generator : RigEquipmentBase
    {
        public GeneratorClass? GeneratorClass { get; set; }
        public double? Speed { get; set; }
        public double? Power { get; set; }
        public double? Voltage { get; set; }
        public double? PowerFactor { get; set; }
        public SpeedMode? SpeedMode { get; set; }
        public EngineModelType? EngineModel { get; set; }
        public int? PowerplantGeneratorNumber { get; set; }
        public double? PowerplantTotalPower { get; set; }
        public double? StartupTimeCold { get; set; }
        public double? StartupTimeWarm { get; set; }
        public GeneratorCooling? CoolingMedium { get; set; }
        public GeneratorPhases? Phases { get; set; }
        public double? MaxLimitPower { get; set; }
        public double? MaxLimitPowerIncrease { get; set; }
        public double? MaxLimitSpeedIncrease { get; set; }
        public double? MaxLimitSpeed { get; set; }
        public double? MaxLimitVoltage { get; set; }
        public double? MinLimitVoltage { get; set; }
        public double? MaxLimitFrequency { get; set; }
        public double? MinLimitFrequency { get; set; }
        public double? EnginePower { get; set; }
        public double? GeneratorPower { get; set; }
        public double? EngineFuelConsumption { get; set; }
        public double? EngineSpecificFuelConsumption { get; set; }
        public double? RunningHours { get; set; }
        public double? EngineSpeed { get; set; }
        public double? GeneratorVoltage { get; set; }
        public double? GridVoltage { get; set; }
        public double? GridFrequency { get; set; }
        public double? GeneratorFrequency { get; set; }
        public double? EngineTemperature { get; set; }

        public Generator() { }
    }
}
