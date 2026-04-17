using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RigModel = NORCE.Drilling.Rig.ModelShared;

namespace NORCE.Drilling.Rig.WebPages.Shared;

public sealed class RigApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;

    public RigApiClient(IRigAPIUtils api)
    {
        _httpClient = api.CreateHttpClient(api.HostNameRig, api.HostBasePathRig);
    }

    public async Task<List<Guid>> GetRigIdsAsync() =>
        await GetAsync<List<Guid>>("Rig") ?? new List<Guid>();

    public async Task<List<RigModel.MetaInfo?>> GetRigMetaInfosAsync() =>
        await GetAsync<List<RigModel.MetaInfo?>>("Rig/MetaInfo") ?? new List<RigModel.MetaInfo?>();

    public async Task<List<RigModel.RigLight>> GetRigLightsAsync() =>
        await GetAsync<List<RigModel.RigLight>>("Rig/LightData") ?? new List<RigModel.RigLight>();

    public async Task<List<RigModel.Rig?>> GetRigsAsync() =>
        await GetAsync<List<RigModel.Rig?>>("Rig/HeavyData") ?? new List<RigModel.Rig?>();

    public Task<RigModel.Rig?> GetRigAsync(Guid id) => GetAsync<RigModel.Rig>($"Rig/{id}");

    public Task<RigModel.UsageStatisticsRig?> GetUsageStatisticsAsync() => GetAsync<RigModel.UsageStatisticsRig>("RigUsageStatistics");

    public Task<HttpStatusCode> CreateRigAsync(RigModel.Rig rig) => SendAsync(HttpMethod.Post, "Rig", rig);

    public Task<HttpStatusCode> UpdateRigAsync(RigModel.Rig rig) => SendAsync(HttpMethod.Put, $"Rig/{rig.MetaInfo?.ID}", rig);

    public async Task<HttpStatusCode> DeleteRigAsync(Guid id)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"Rig/{id}");
        return response.StatusCode;
    }

    private async Task<T?> GetAsync<T>(string relativeUrl)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(relativeUrl);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task<HttpStatusCode> SendAsync(HttpMethod method, string relativeUrl, RigModel.Rig rig)
    {
        using HttpRequestMessage request = new(method, relativeUrl)
        {
            Content = JsonContent.Create(rig, options: JsonOptions)
        };
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        return response.StatusCode;
    }
}
