using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OSDC.Drilling.Rig.Model;

namespace OSDC.Drilling.Rig.Service;

public sealed class RigExternalReferenceResolutionOutcome
{
    public List<RigBatchExternalReferenceMapping> Mappings { get; init; } = [];
    public List<RigBatchError> Errors { get; init; } = [];
    public bool IsSuccess => Errors.Count == 0;
}

public interface IRigExternalReferenceResolver
{
    Task<bool> ClusterExistsAsync(Guid clusterId, CancellationToken cancellationToken);
    Task<List<RigBatchError>> PopulateExportManifestAsync(RigBatchExportDocument document, CancellationToken cancellationToken);
    Task<RigExternalReferenceResolutionOutcome> ResolveRestoreManifestAsync(RigBatchExportDocument document, CancellationToken cancellationToken);
}

/// <summary>Resolves Cluster references live and never creates or modifies Cluster resources.</summary>
public sealed class RigExternalReferenceResolver : IRigExternalReferenceResolver
{
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;
    public RigExternalReferenceResolver(IHttpClientFactory clients, IConfiguration configuration)
    { _clients = clients; _configuration = configuration; }

    public async Task<bool> ClusterExistsAsync(Guid clusterId, CancellationToken cancellationToken)
    {
        if (clusterId == Guid.Empty) return false;
        using HttpClient client = CreateClusterClient();
        using HttpResponseMessage response = await client.GetAsync($"Cluster/api/Cluster/{clusterId:D}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<List<RigBatchError>> PopulateExportManifestAsync(RigBatchExportDocument document, CancellationToken cancellationToken)
    {
        List<RigBatchError> errors = [];
        for (int index = 0; index < document.Rigs.Count; index++)
            if (document.Rigs[index].ClusterID == Guid.Empty)
                errors.Add(Error(index, "Document.Rigs.ClusterID", "empty_uuid", "ClusterID must be null or a non-empty UUID."));
        IReadOnlyList<ExternalResource> clusters = document.Rigs.Any(value => IsReference(value.ClusterID))
            ? await ReadClustersAsync(cancellationToken) : [];
        document.ExternalReferences = new RigBatchExternalReferences
        {
            Clusters = BuildManifest(document.Rigs.Select(value => value.ClusterID), clusters, errors)
        };
        return errors;
    }

    public async Task<RigExternalReferenceResolutionOutcome> ResolveRestoreManifestAsync(RigBatchExportDocument document, CancellationToken cancellationToken)
    {
        IReadOnlyList<ExternalResource> locals = document.ExternalReferences.Clusters.Count == 0
            ? [] : await ReadClustersAsync(cancellationToken);
        List<RigBatchExternalReferenceMapping> mappings = [];
        List<RigBatchError> errors = [];
        foreach (RigBatchExternalReference source in document.ExternalReferences.Clusters)
        {
            ExternalResource? exact = locals.SingleOrDefault(value => value.ID == source.SourceID);
            if (exact is not null) { mappings.Add(Mapping(source, exact.ID, "ExactUUID")); continue; }
            List<ExternalResource> matches = locals.Where(value => SameName(value.Name, source.Name)).ToList();
            if (matches.Count == 1) mappings.Add(Mapping(source, matches[0].ID, "NormalizedName"));
            else if (matches.Count == 0) errors.Add(Error(null, $"Document.ExternalReferences.Clusters[{source.SourceID}]", "external_reference_not_found", $"No destination Cluster named '{source.Name}' exists."));
            else errors.Add(Error(null, $"Document.ExternalReferences.Clusters[{source.SourceID}]", "ambiguous_external_reference", $"More than one destination Cluster has normalized name '{source.Name}'."));
        }
        foreach (IGrouping<Guid, RigBatchExternalReferenceMapping> collision in mappings.GroupBy(value => value.LocalID)
                     .Where(group => group.Select(value => value.SourceID).Distinct().Count() > 1))
            errors.Add(Error(null, "Document.ExternalReferences.Clusters", "external_reference_collision", $"Several source Clusters resolve to destination UUID '{collision.Key}'."));
        return new RigExternalReferenceResolutionOutcome { Mappings = mappings, Errors = errors };
    }

    private async Task<IReadOnlyList<ExternalResource>> ReadClustersAsync(CancellationToken cancellationToken)
    {
        using HttpClient client = CreateClusterClient();
        using HttpResponseMessage response = await client.GetAsync("Cluster/api/Cluster/LightData", cancellationToken);
        response.EnsureSuccessStatusCode();
        List<ExternalResourceDto>? values = await response.Content.ReadFromJsonAsync<List<ExternalResourceDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return (values ?? []).Where(value => value.MetaInfo?.ID is Guid id && id != Guid.Empty)
            .Select(value => new ExternalResource(value.MetaInfo!.ID, value.Name ?? string.Empty)).ToList();
    }

    private HttpClient CreateClusterClient()
    {
        string? host = _configuration["ClusterHostURL"];
        if (string.IsNullOrWhiteSpace(host)) throw new HttpRequestException("ClusterHostURL is not configured for Cluster reference resolution.");
        HttpClient client = _clients.CreateClient(nameof(RigExternalReferenceResolver));
        client.BaseAddress = new Uri(host.EndsWith('/') ? host : host + "/");
        return client;
    }

    private static List<RigBatchExternalReference> BuildManifest(IEnumerable<Guid?> ids, IReadOnlyList<ExternalResource> available, List<RigBatchError> errors)
    {
        Dictionary<Guid, ExternalResource> byId = available.GroupBy(value => value.ID).ToDictionary(group => group.Key, group => group.First());
        List<RigBatchExternalReference> result = [];
        foreach (Guid id in ids.Where(IsReference).Select(value => value!.Value).Distinct().Order())
            if (!byId.TryGetValue(id, out ExternalResource? value)) errors.Add(Error(null, "Document.ExternalReferences.Clusters", "external_reference_not_found", $"Referenced Cluster UUID '{id}' does not exist on the source service."));
            else if (string.IsNullOrWhiteSpace(value.Name)) errors.Add(Error(null, "Document.ExternalReferences.Clusters", "external_reference_name_missing", $"Referenced Cluster UUID '{id}' has no usable name."));
            else result.Add(new RigBatchExternalReference { SourceID = id, Name = value.Name });
        return result;
    }

    private static RigBatchExternalReferenceMapping Mapping(RigBatchExternalReference source, Guid localId, string resolution) =>
        new() { Resource = "Cluster", Name = source.Name, SourceID = source.SourceID, LocalID = localId, Resolution = resolution };
    private static bool IsReference(Guid? value) => value is Guid id && id != Guid.Empty;
    private static string Normalize(string? value) => string.Join(' ', (value ?? string.Empty).Normalize(NormalizationForm.FormKC)
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    private static bool SameName(string? left, string? right) => Normalize(left) == Normalize(right);
    private static RigBatchError Error(int? index, string property, string code, string message) =>
        new() { PositionIndex = index, Property = property, Code = code, Message = message };
    private sealed class ExternalResourceDto { public ExternalMetaInfo? MetaInfo { get; set; } public string? Name { get; set; } }
    private sealed class ExternalMetaInfo { public Guid ID { get; set; } }
    private sealed record ExternalResource(Guid ID, string Name);
}
