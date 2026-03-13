namespace NORCE.Drilling.Rig.Model
{
    public class HoistingSystem : RigComponentBase
    {
        public HoistingSystemType? HoistingSystemType { get; set; }
        public Drawworks? Drawworks { get; set; }
        public CrownBlock? CrownBlock { get; set; }
        public TravellingBlock? TravellingBlock { get; set; }
        public DrillLine? DrillLine { get; set; }

        public HoistingSystem() { }
    }
}
