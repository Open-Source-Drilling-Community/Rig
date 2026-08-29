using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace OSDC.Drilling.Rig.WebPages;

public interface IRigWebPagesConfiguration :
    IRigHostURL,
    IUnitConversionHostURL,
    IFieldHostURL,
    IClusterHostURL
{
    string VerticalDatumHostURL { get; set; }
}
