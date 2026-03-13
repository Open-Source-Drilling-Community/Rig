using System.Collections.Generic;

namespace NORCE.Drilling.Rig.Model
{
    public class ShaleShaker : RigEquipmentBase
    {
        public ShakerClass? ShakerClass { get; set; }
        public string? ActiveShakers { get; set; }
        public List<ShakerScreenDefinition>? ShakerScreens { get; set; }
        public double? MaxLimitOperatingCapacity { get; set; }

        public ShaleShaker() { }
    }
}



