using System.Collections.Generic;

namespace OSDC.Drilling.Rig.Model
{
    public class CementUnit : RigEquipmentBase
    {
        public MountingType? Mounting { get; set; }
        public List<string>? Capabilities { get; set; }
        public int? NumberOfPumps { get; set; }

        public CementUnit() { }
    }
}



