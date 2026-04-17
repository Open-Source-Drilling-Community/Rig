using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Rig.WebPages;

public interface IRigWebPagesConfiguration :
    IRigHostURL,
    IUnitConversionHostURL,
    IFieldHostURL,
    IClusterHostURL
{
}
