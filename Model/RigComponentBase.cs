namespace NORCE.Drilling.Rig.Model
{
    public abstract class RigComponentBase
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public abstract class RigEquipmentBase : RigComponentBase
    {
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? ProductCode { get; set; }
        public string? SerialNumber { get; set; }
    }
}
