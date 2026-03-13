using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NORCE.Drilling.Rig.ModelShared;

public sealed class RigApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;

    public RigApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Guid>> GetRigIdsAsync() =>
        await GetAsync<List<Guid>>("Rig") ?? new List<Guid>();

    public async Task<List<MetaInfo?>> GetRigMetaInfosAsync() =>
        await GetAsync<List<MetaInfo?>>("Rig/MetaInfo") ?? new List<MetaInfo?>();

    public async Task<List<RigLight>> GetRigLightsAsync() =>
        await GetAsync<List<RigLight>>("Rig/LightData") ?? new List<RigLight>();

    public async Task<List<Rig?>> GetRigsAsync() =>
        await GetAsync<List<Rig?>>("Rig/HeavyData") ?? new List<Rig?>();

    public Task<Rig?> GetRigAsync(Guid id) => GetAsync<Rig>($"Rig/{id}");

    public Task<UsageStatisticsRig?> GetUsageStatisticsAsync() => GetAsync<UsageStatisticsRig>("RigUsageStatistics");

    public Task<HttpStatusCode> CreateRigAsync(Rig rig) => SendAsync(HttpMethod.Post, "Rig", rig);

    public Task<HttpStatusCode> UpdateRigAsync(Rig rig) => SendAsync(HttpMethod.Put, $"Rig/{rig.MetaInfo?.ID}", rig);

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

    private async Task<HttpStatusCode> SendAsync(HttpMethod method, string relativeUrl, Rig rig)
    {
        using HttpRequestMessage request = new(method, relativeUrl)
        {
            Content = JsonContent.Create(rig, options: JsonOptions)
        };
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        return response.StatusCode;
    }
}
