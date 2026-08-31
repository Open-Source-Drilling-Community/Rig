using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Rig.Model
{
    public class RigFeatureCategory
    {
        public MetaInfo? MetaInfo { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsExclusive { get; set; }
        public bool HasValidityPeriod { get; set; }
        public bool IsBuiltIn { get; set; }
        public bool IsDeprecated { get; set; }
        public List<RigFeatureOption>? Options { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? LastModificationDate { get; set; }
    }
}
