using NORCE.Drilling.Rig.WebPages;

namespace NORCE.Drilling.Rig.WebApp;

public class WebPagesHostConfiguration : IRigWebPagesConfiguration
{
    public string RigHostURL { get; set; } = string.Empty;
    public string UnitConversionHostURL { get; set; } = string.Empty;
    public string FieldHostURL { get; set; } = string.Empty;
    public string ClusterHostURL { get; set; } = string.Empty;
}
