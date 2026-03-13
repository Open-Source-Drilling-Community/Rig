namespace NORCE.Drilling.Rig.Model
{
    public class TorqueTurnSub : RigEquipmentBase
    {
        public double? Length { get; set; }
        public double? OutsideDiameter { get; set; }
        public double? InsideDiameter { get; set; }
        public double? Weight { get; set; }
        public double? BatteryLife { get; set; }
        public double? MaxLimitDesignLoad { get; set; }
        public double? MaxLimitDesignTorque { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitLoad { get; set; }
        public double? MaxLimitTorque { get; set; }
        public double? MaxLimitPressure { get; set; }
        public double? MaxLimitTemperature { get; set; }
        public double? MinLimitTemperature { get; set; }
        public double? SurfaceTorque { get; set; }
        public double? Hookload { get; set; }
        public int? SurfaceTurnCount { get; set; }
        public double? SurfaceAcceleration { get; set; }
        public double? SurfaceRotationRate { get; set; }
        public double? SurfaceBorePressure { get; set; }
        public double? SurfaceAxialVibration { get; set; }
        public double? SurfaceTorsionalVibration { get; set; }
        public double? SurfaceLateralVibration { get; set; }

        public TorqueTurnSub() { }
    }
}



