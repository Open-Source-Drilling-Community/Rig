using System.Net.Http.Headers;
using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Rig.WebPages;

public class RigAPIUtils : APIUtils, IRigAPIUtils
{
    public RigAPIUtils(IRigWebPagesConfiguration configuration)
    {
        HostNameRig = Require(configuration.RigHostURL, nameof(configuration.RigHostURL));
        HostNameUnitConversion = Require(configuration.UnitConversionHostURL, nameof(configuration.UnitConversionHostURL));
        HostNameField = Require(configuration.FieldHostURL, nameof(configuration.FieldHostURL));
        HostNameCluster = Require(configuration.ClusterHostURL, nameof(configuration.ClusterHostURL));
    }

    private static string Require(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{propertyName}' must be assigned before WebPages is used.");
        }

        return value;
    }

    public string HostNameRig { get; }
    public string HostBasePathRig => "Rig/api/";
    public string HostNameUnitConversion { get; }
    public string HostBasePathUnitConversion => "UnitConversion/api/";
    public string HostNameField { get; }
    public string HostBasePathField => "Field/api/";
    public string HostNameCluster { get; }
    public string HostBasePathCluster => "Cluster/api/";

    public HttpClient CreateHttpClient(string host, string microServiceUri)
    {
        HttpClientHandler handler = new();
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri(new Uri(host), microServiceUri)
        };
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return httpClient;
    }
}
