using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OSDC.Drilling.Rig.Service.Mcp;
using OSDC.Drilling.Rig.Service.Mcp.Tools;

namespace ServiceTest;

[TestFixture]
[NonParallelizable]
public sealed class McpServerHttpTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _httpClient = null!;
    private HttpClientTransport _transport = null!;
    private McpClient _client = null!;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Service")));
            builder.ConfigureLogging(logging => logging.ClearProviders());
        });
        _httpClient = _factory.CreateClient();
        _transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(_httpClient.BaseAddress!, "Rig/api/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp
        }, _httpClient, NullLoggerFactory.Instance, ownsHttpClient: false);
        _client = await McpClient.CreateAsync(_transport, new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "RigServiceTest", Version = "1.0.0" }
        }, NullLoggerFactory.Instance, CancellationToken.None);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (_client is not null) await _client.DisposeAsync();
        if (_transport is not null) await _transport.DisposeAsync();
        _httpClient?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Http_endpoint_publishes_every_registered_non_statistics_tool()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLegacyMcpTool<PingMcpTool>();
        services.AddRigRestMcpTools();
        using var provider = services.BuildServiceProvider();
        var expected = provider.GetServices<McpServerTool>().Select(tool => tool.ProtocolTool.Name);
        var remote = (await _client.ListToolsAsync(cancellationToken: CancellationToken.None)).Select(tool => tool.Name).ToArray();
        Assert.That(remote, Is.EquivalentTo(expected));
        Assert.That(remote, Has.None.Contains("statistics"));
    }

    [Test]
    public async Task Ping_can_be_invoked_over_http()
    {
        var result = await _client.CallToolAsync("ping", new Dictionary<string, object?>(), cancellationToken: CancellationToken.None);
        Assert.That(((JsonObject)result.StructuredContent!)["message"]?.GetValue<string>(), Is.EqualTo("pong"));
    }

    [Test]
    public async Task Validation_failure_is_json_text_error_without_success_structured_content()
    {
        CallToolResult result = await _client.CallToolAsync("rig_get_by_id", new Dictionary<string, object?>(), cancellationToken: CancellationToken.None);
        Assert.That(result.IsError, Is.True);
        Assert.That(result.StructuredContent, Is.Null);
        TextContentBlock text = (TextContentBlock)result.Content.Single();
        JsonObject envelope = (JsonObject)JsonNode.Parse(text.Text)!;
        Assert.That(envelope["error"]?.GetValue<string>(), Is.EqualTo("validation_failed"));
        Assert.That(envelope["errors"], Is.TypeOf<JsonArray>());
    }
}
