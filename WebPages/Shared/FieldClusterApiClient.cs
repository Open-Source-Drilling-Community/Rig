using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OSDC.Drilling.Rig.ModelShared;
using RigShared = OSDC.Drilling.Rig.ModelShared;

namespace OSDC.Drilling.Rig.WebPages.Shared;

public sealed class FieldClusterApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _fieldHttpClient;
    private readonly HttpClient _clusterHttpClient;

    public FieldClusterApiClient(IRigAPIUtils api)
    {
        _fieldHttpClient = api.CreateHttpClient(api.HostNameField, api.HostBasePathField);
        _clusterHttpClient = api.CreateHttpClient(api.HostNameCluster, api.HostBasePathCluster);
    }

    public async Task<List<RigShared.Field>> GetFieldsAsync() =>
        await new RigShared.Client(_fieldHttpClient.BaseAddress!.ToString(), _fieldHttpClient).ListFieldReferencesAsync();

    public async Task<List<RigShared.Cluster>> GetClustersAsync() =>
        await new RigShared.Client(_clusterHttpClient.BaseAddress!.ToString(), _clusterHttpClient).ListClusterReferencesAsync();

    private static async Task<T?> GetAsync<T>(HttpClient httpClient, string relativeUrl)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(relativeUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }
}
