using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Rig.Model
{
    /// <summary>Stable identity, ownership, construction, and classification information for a rig.</summary>
    public class RigIdentification
    {
        public string? Owner { get; set; }
        public string? Operator { get; set; }
        public string? ManufacturerOrShipyard { get; set; }
        public string? DesignName { get; set; }
        public int? YearBuilt { get; set; }
        public int? YearEnteredService { get; set; }
        public string? Registration { get; set; }
        public string? Flag { get; set; }
        public string? ClassificationSociety { get; set; }
        public string? ClassNotation { get; set; }
        public List<string>? ApprovalsAndCertifications { get; set; }
        public List<string>? FormerNames { get; set; }
        public List<RigExternalIdentifier>? ExternalIdentifiers { get; set; }
        public List<RigModification>? MajorModifications { get; set; }
    }

    public class RigExternalIdentifier
    {
        public string? Authority { get; set; }
        public string? Identifier { get; set; }
    }

    public class RigModification
    {
        public DateTimeOffset? Date { get; set; }
        public string? Description { get; set; }
    }
}
