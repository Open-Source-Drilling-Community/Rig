using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OSDC.Drilling.Rig.Model
{
    public class MarineUnitProfile
    {
        public double? HullLength { get; set; }
        public double? HullWidth { get; set; }
        public double? HullDepth { get; set; }
        public double? OperatingDraft { get; set; }
        public double? TransitDraft { get; set; }
        public double? OperatingDisplacement { get; set; }
        public double? VariableDeckLoad { get; set; }
        public double? MaximumTransitSpeed { get; set; }
        public int? AccommodationCapacity { get; set; }
        public string? HelideckCapability { get; set; }
        public int? CraneCount { get; set; }
    }

    public class JackUpProfile
    {
        public double? LegLength { get; set; }
        public double? LongitudinalLegSpacing { get; set; }
        public double? TransverseLegSpacing { get; set; }
        public double? MaximumCantileverSkidOut { get; set; }
        public double? MaximumCantileverTransverseReach { get; set; }
        public double? SubstructureTravel { get; set; }
        public double? MaximumPreload { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StationKeepingMode
    {
        Unknown,
        Fixed,
        SelfElevating,
        Moored,
        DynamicPositioning
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DynamicPositioningClass
    {
        None,
        DP1,
        DP2,
        DP3
    }

    public class StationKeepingSystem
    {
        public List<StationKeepingMode>? Modes { get; set; }
        public DynamicPositioningClass? DynamicPositioningClass { get; set; }
        public int? ThrusterCount { get; set; }
        public int? MooringLineCount { get; set; }
        public double? MaximumMooringLineTension { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RigStorageType
    {
        Other,
        DieselFuel,
        DrillWater,
        PotableWater,
        ActiveDrillingFluid,
        ReserveDrillingFluid,
        BaseOil,
        Brine,
        BulkCement,
        BulkBarite,
        BulkBentonite,
        Cuttings
    }

    public class RigStorageCapacity
    {
        public RigStorageType StorageType { get; set; }
        public string? Name { get; set; }
        public double? MaximumVolume { get; set; }
        public double? MaximumMass { get; set; }
    }
}
