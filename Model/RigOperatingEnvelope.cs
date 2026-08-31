namespace OSDC.Drilling.Rig.Model
{
    /// <summary>Certified or advertised rig-level capability limits, expressed in SI units.</summary>
    public class RigOperatingEnvelope
    {
        public double? MaximumDrillingDepth { get; set; }
        public double? MaximumWaterDepth { get; set; }
        public double? RatedHookLoad { get; set; }
        public double? MaximumSetbackLoad { get; set; }
        public double? MaximumRotaryLoad { get; set; }
        public double? MaximumMudSystemPressure { get; set; }
        public double? MinimumAmbientTemperature { get; set; }
        public double? MaximumAmbientTemperature { get; set; }
        public double? MaximumOperatingWindSpeed { get; set; }
        public double? MaximumSurvivalWindSpeed { get; set; }
    }
}
