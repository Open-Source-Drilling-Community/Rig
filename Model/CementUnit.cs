namespace NORCE.Drilling.Rig.Model
{
    public class CementUnit : RigEquipmentBase
    {
        public MountingType? Mounting { get; set; }
        public string? Features { get; set; }
        public int? NumberOfPumps { get; set; }

        public CementUnit() { }
    }
}



