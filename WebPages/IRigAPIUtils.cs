namespace NORCE.Drilling.Rig.WebPages;

public interface IRigAPIUtils
{
    string HostNameRig { get; }
    string HostBasePathRig { get; }
    string HostNameUnitConversion { get; }
    string HostBasePathUnitConversion { get; }
    string HostNameField { get; }
    string HostBasePathField { get; }
    string HostNameCluster { get; }
    string HostBasePathCluster { get; }
    HttpClient CreateHttpClient(string host, string microServiceUri);
}
