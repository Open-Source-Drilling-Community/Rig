using System;
using System.Net.Http;
using System.Net.Http.Headers;

public static class APIUtils
{
    public static string HostNameRig => NORCE.Drilling.Rig.WebApp.Configuration.RigHostURL ?? "https://localhost:5001/";
    public const string HostBasePathRig = "Rig/api/";

    public static string HostNameUnitConversion => NORCE.Drilling.Rig.WebApp.Configuration.UnitConversionHostURL ?? "https://dev.digiwells.no/";
    public const string HostBasePathUnitConversion = "UnitConversion/api/";

    public static string HostNameField => NORCE.Drilling.Rig.WebApp.Configuration.FieldHostURL ?? "https://dev.digiwells.no/";
    public const string HostBasePathField = "Field/api/";

    public static string HostNameCluster => NORCE.Drilling.Rig.WebApp.Configuration.ClusterHostURL ?? "https://dev.digiwells.no/";
    public const string HostBasePathCluster = "Cluster/api/";

    public static HttpClient CreateHttpClient(string host, string microServiceUri)
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
