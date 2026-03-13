namespace NORCE.Drilling.Rig.Model
{
    public class MudTank : RigEquipmentBase
    {
        public TankClass? TankClass { get; set; }
        public TankFluidType? TankFluidType { get; set; }
        public double? MaxLimitOperatingVolume { get; set; }

        public MudTank() { }
    }
}
