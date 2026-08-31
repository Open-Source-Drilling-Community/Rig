using System;

namespace OSDC.Drilling.Rig.Model
{
    public class RigFeatureAssignment
    {
        public Guid ID { get; set; }
        public Guid FeatureCategoryID { get; set; }
        public Guid FeatureOptionID { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public string? Notes { get; set; }
        public string? EvidenceReference { get; set; }
    }
}
