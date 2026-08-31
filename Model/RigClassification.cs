using System.Text.Json.Serialization;

namespace OSDC.Drilling.Rig.Model
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RigType
    {
        Unknown,
        ConventionalLandRig,
        MobileLandRig,
        PlatformRig,
        JackUp,
        Semisubmersible,
        Drillship,
        TenderAssistedRig,
        DrillingBarge
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RigEnvironment
    {
        Unknown,
        Onshore,
        Offshore,
        InlandWater
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RigMobilityType
    {
        Unknown,
        Fixed,
        Mobile,
        SelfPropelled,
        NonSelfPropelled
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EquipmentLifecycleStatus
    {
        Unknown,
        Planned,
        Installed,
        InService,
        Suspended,
        UnderMaintenance,
        Retired
    }
}
