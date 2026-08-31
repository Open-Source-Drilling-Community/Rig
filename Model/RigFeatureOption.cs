using System;

namespace OSDC.Drilling.Rig.Model
{
    public class RigFeatureOption
    {
        public Guid ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsBuiltIn { get; set; }
        public bool IsDeprecated { get; set; }
    }
}
