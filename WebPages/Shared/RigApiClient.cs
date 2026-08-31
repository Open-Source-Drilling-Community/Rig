using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RigModel = OSDC.Drilling.Rig.ModelShared;

namespace OSDC.Drilling.Rig.WebPages.Shared;

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

    public async Task<List<RigModel.RigPhotoMetadata>> GetRigPhotosAsync(Guid rigId) =>
        await GetAsync<List<RigModel.RigPhotoMetadata>>($"Rig/{rigId}/Photos") ?? [];

    public async Task<string> GetRigPhotoDataUrlAsync(Guid rigId, Guid photoId)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"Rig/{rigId}/Photos/{photoId}/Content");
        response.EnsureSuccessStatusCode();
        string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return $"data:{contentType};base64,{Convert.ToBase64String(await response.Content.ReadAsByteArrayAsync())}";
    }

    public async Task<RigModel.RigPhotoMetadata> UploadRigPhotoAsync(Guid rigId, Stream stream, string fileName, string contentType, RigModel.RigPhotoMetadata metadata)
    {
        using MultipartFormDataContent form = new();
        StreamContent file = new(stream); file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType); form.Add(file, "file", fileName);
        void Add(string name, object? value) { if (value != null) form.Add(new StringContent(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!), name); }
        Add("title", metadata.Title); Add("caption", metadata.Caption); Add("alternativeText", metadata.AlternativeText); Add("displayOrder", metadata.DisplayOrder); Add("isPrimary", metadata.IsPrimary); Add("source", metadata.Source); Add("attribution", metadata.Attribution); Add("license", metadata.License);
        using HttpResponseMessage response = await _httpClient.PostAsync($"Rig/{rigId}/Photos", form); response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RigModel.RigPhotoMetadata>(JsonOptions))!;
    }

    public async Task<RigModel.RigPhotoMetadata> UpdateRigPhotoAsync(Guid rigId, RigModel.RigPhotoMetadata metadata)
    {
        if (metadata.MetaInfo == null || metadata.LastModificationDate == null) throw new InvalidOperationException("Stored photo metadata is required.");
        string expected = Uri.EscapeDataString(metadata.LastModificationDate.Value.ToString("O"));
        using HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"Rig/{rigId}/Photos/{metadata.MetaInfo.ID}?expectedModifiedUtc={expected}", metadata, JsonOptions); response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RigModel.RigPhotoMetadata>(JsonOptions))!;
    }

    public async Task<HttpStatusCode> DeleteRigPhotoAsync(Guid rigId, Guid photoId)
    { using HttpResponseMessage response = await _httpClient.DeleteAsync($"Rig/{rigId}/Photos/{photoId}"); return response.StatusCode; }

    public async Task<List<RigModel.RigFeatureCategory>> GetRigFeatureCategoriesAsync() =>
        await GetAsync<List<RigModel.RigFeatureCategory>>("RigFeatureCategory/HeavyData") ?? new List<RigModel.RigFeatureCategory>();

    public async Task<RigModel.RigFeatureCategory> CreateRigFeatureCategoryAsync(RigModel.RigFeatureCategory category)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("RigFeatureCategory", category, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RigModel.RigFeatureCategory>(JsonOptions))!;
    }

    public async Task<RigModel.RigFeatureCategory> UpdateRigFeatureCategoryAsync(RigModel.RigFeatureCategory category)
    {
        if (category.MetaInfo == null || category.MetaInfo.ID == Guid.Empty || category.LastModificationDate == null)
            throw new InvalidOperationException("A stored category UUID and LastModificationDate are required for update.");
        string timestamp = Uri.EscapeDataString(category.LastModificationDate.Value.ToString("O"));
        using HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"RigFeatureCategory/{category.MetaInfo.ID}?expectedModifiedUtc={timestamp}", category, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RigModel.RigFeatureCategory>(JsonOptions))!;
    }

    public async Task<HttpStatusCode> DeleteRigFeatureCategoryAsync(Guid id)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"RigFeatureCategory/{id}");
        return response.StatusCode;
    }

    public Task<RigModel.UsageStatisticsRig?> GetUsageStatisticsAsync() => GetAsync<RigModel.UsageStatisticsRig>("RigUsageStatistics");

    public Task<RigModel.RigBatchExportDocument> BatchExportAsync(RigModel.RigBatchExportRequest request) =>
        PostBatchAsync<RigModel.RigBatchExportRequest, RigModel.RigBatchExportDocument>("Rig/BatchExport", request);

    public Task<RigModel.RigBatchRestoreResponse> BatchRestoreAsync(RigModel.RigBatchRestoreRequest request) =>
        PostBatchAsync<RigModel.RigBatchRestoreRequest, RigModel.RigBatchRestoreResponse>("Rig/BatchRestore", request);

    public Task<HttpStatusCode> CreateRigAsync(RigModel.Rig rig) => SendAsync(HttpMethod.Post, "Rig", rig);

    public Task<HttpStatusCode> UpdateRigAsync(RigModel.Rig rig)
    {
        if (rig.MetaInfo?.ID is not Guid id || id == Guid.Empty || rig.LastModificationDate is not DateTimeOffset modified)
            throw new InvalidOperationException("A stored rig UUID and LastModificationDate are required for update.");
        string expected = Uri.EscapeDataString(modified.ToString("O"));
        return SendAsync(HttpMethod.Put, $"Rig/{id}?expectedModifiedUtc={expected}", rig);
    }

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

    private async Task<TResponse> PostBatchAsync<TRequest, TResponse>(string relativeUrl, TRequest request)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(relativeUrl, request, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            RigModel.RigBatchErrorEnvelope? error = null;
            try { error = await response.Content.ReadFromJsonAsync<RigModel.RigBatchErrorEnvelope>(JsonOptions); } catch { }
            throw new RigBatchApiException(response.StatusCode, error);
        }
        return (await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions))!;
    }
}

public sealed class RigBatchApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public RigModel.RigBatchErrorEnvelope? Error { get; }
    public RigBatchApiException(HttpStatusCode statusCode, RigModel.RigBatchErrorEnvelope? error)
        : base(error?.Message ?? $"Rig batch request failed with HTTP {(int)statusCode}.")
    { StatusCode = statusCode; Error = error; }
}
