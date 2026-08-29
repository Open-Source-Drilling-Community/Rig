using System.Text.Json.Serialization;

namespace OSDC.Drilling.Rig.Model
{
    /// <summary>
    /// Describes the mud pump variants represented by <see cref="MudPump.Type"/>.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MudPumpType
    {
        Variant1,
        Variant2
    }
}
