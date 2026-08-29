using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NORCE.Drilling.Rig.Service.Managers;

namespace NORCE.Drilling.Rig.Service.Mcp;

public sealed class McpHubRegistrationService : BackgroundService
{
    public static readonly Guid ServiceTypeId = Guid.Parse("59017b10-2492-45f0-9843-c2d1600e03a8");
    private const string InstanceIdFileName = "rig-mcp-hub-instance-id.txt";
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<McpHubRegistrationService> _logger;
    private readonly IOptionsMonitor<McpHubOptions> _options;
    private Guid? _registeredInstanceId;

    public McpHubRegistrationService(IHttpClientFactory clients, ILogger<McpHubRegistrationService> logger, IOptionsMonitor<McpHubOptions> options)
    {
        _clients = clients;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.CurrentValue;
        if (!options.Enabled) { _logger.LogInformation("MCP hub registration is disabled."); return; }
        if (!IsComplete(options)) { _logger.LogWarning("MCP hub registration skipped because its URLs are not configured."); return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            options = _options.CurrentValue;
            if (!options.Enabled || !IsComplete(options)) return;
            try
            {
                var instanceId = ResolveInstanceId(options);
                var collectionUri = CreateCollectionUri(options);
                var registration = CreateRegistration(options, instanceId);
                using var client = _clients.CreateClient(nameof(McpHubRegistrationService));
                using var response = await PutOrPostAsync(client, collectionUri, instanceId, registration, stoppingToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    _registeredInstanceId = instanceId;
                    _logger.LogInformation("Registered Rig MCP endpoint on MCP hub at {HubUri}", collectionUri);
                }
                else
                    _logger.LogWarning("MCP hub registration failed with status {StatusCode}.", response.StatusCode);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { _logger.LogWarning(ex, "MCP hub registration attempt failed."); }

            try { await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.RetryIntervalSeconds)), stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (options.UnregisterOnShutdown && _registeredInstanceId.HasValue && !string.IsNullOrWhiteSpace(options.HubBaseUrl))
        {
            try
            {
                using var client = _clients.CreateClient(nameof(McpHubRegistrationService));
                using var response = await client.DeleteAsync(new Uri(CreateCollectionUri(options), _registeredInstanceId.Value.ToString()), cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                    _logger.LogWarning("MCP hub unregister failed with status {StatusCode}.", response.StatusCode);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex) { _logger.LogWarning(ex, "MCP hub unregister failed during shutdown."); }
        }
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<HttpResponseMessage> PutOrPostAsync(HttpClient client, Uri collectionUri, Guid instanceId, Registration registration, CancellationToken ct)
    {
        var response = await client.PutAsJsonAsync(new Uri(collectionUri, instanceId.ToString()), registration, ct).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.NotFound) return response;
        response.Dispose();
        return await client.PostAsJsonAsync(collectionUri, registration, ct).ConfigureAwait(false);
    }

    private static bool IsComplete(McpHubOptions options) => !string.IsNullOrWhiteSpace(options.HubBaseUrl) && !string.IsNullOrWhiteSpace(options.PublicBaseUrl);
    private static Registration CreateRegistration(McpHubOptions options, Guid instanceId)
    {
        var publicUrl = options.PublicBaseUrl!.TrimEnd('/');
        return new Registration(ServiceTypeId, instanceId, string.IsNullOrWhiteSpace(options.ServiceName) ? "Rig" : options.ServiceName,
            $"{publicUrl}/Rig/api/mcp", ToWebSocketUrl($"{publicUrl}/Rig/api/mcp/ws"), DateTimeOffset.UtcNow);
    }
    private static Uri CreateCollectionUri(McpHubOptions options) => new(new Uri(options.HubBaseUrl!.TrimEnd('/') + "/"),
        (string.IsNullOrWhiteSpace(options.RegistrationEndpoint) ? "McpMicroservice" : options.RegistrationEndpoint.Trim('/')) + "/");
    private static Guid ResolveInstanceId(McpHubOptions options)
    {
        if (Guid.TryParse(options.InstanceId, out var configured) && configured != Guid.Empty) return configured;
        Directory.CreateDirectory(SqlConnectionManager.HOME_DIRECTORY);
        var file = Path.Combine(SqlConnectionManager.HOME_DIRECTORY, InstanceIdFileName);
        if (File.Exists(file) && Guid.TryParse(File.ReadAllText(file), out var persisted) && persisted != Guid.Empty) return persisted;
        var generated = Guid.NewGuid();
        File.WriteAllText(file, generated.ToString());
        return generated;
    }
    private static string ToWebSocketUrl(string url) => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        ? "wss://" + url[8..] : url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "ws://" + url[7..] : url;

    private sealed record Registration(Guid ServiceTypeId, Guid InstanceId, string Name, string McpHttpUrl, string McpWebSocketUrl, DateTimeOffset LastSeenUtc);
}
