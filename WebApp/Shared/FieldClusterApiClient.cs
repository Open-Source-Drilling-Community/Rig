using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RigShared = NORCE.Drilling.Rig.ModelShared;

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

    public FieldClusterApiClient()
    {
        _fieldHttpClient = APIUtils.CreateHttpClient(APIUtils.HostNameField, APIUtils.HostBasePathField);
        _clusterHttpClient = APIUtils.CreateHttpClient(APIUtils.HostNameCluster, APIUtils.HostBasePathCluster);
    }

    public async Task<List<RigShared.Field>> GetFieldsAsync() =>
        await GetAsync<List<RigShared.Field>>(_fieldHttpClient, "Field/HeavyData") ?? new List<RigShared.Field>();

    public async Task<List<RigShared.Cluster>> GetClustersAsync() =>
        await GetAsync<List<RigShared.Cluster>>(_clusterHttpClient, "Cluster/HeavyData") ?? new List<RigShared.Cluster>();

    private static async Task<T?> GetAsync<T>(HttpClient httpClient, string relativeUrl)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(relativeUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }
}
