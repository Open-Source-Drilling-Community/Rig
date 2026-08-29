using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace OSDC.Drilling.Rig.Service.Mcp;

internal sealed class LegacyMcpServerToolAdapter : McpServerTool
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IMcpTool _tool;
    private readonly ILogger _logger;
    private readonly Tool _protocolTool;

    public LegacyMcpServerToolAdapter(IMcpTool tool, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _tool = tool;
        _logger = loggerFactory.CreateLogger(tool.GetType());
        _protocolTool = new Tool { Name = tool.Name, Description = tool.Description };
        if (tool.InputSchema is JsonNode schema)
            _protocolTool.InputSchema = JsonSerializer.SerializeToElement(schema, SerializerOptions);
    }

    public override Tool ProtocolTool => _protocolTool;
    public override IReadOnlyList<object> Metadata { get; } = Array.Empty<object>();

    public override async ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            return new CallToolResult { StructuredContent = await _tool.InvokeAsync(ConvertArguments(request.Params?.Arguments), cancellationToken).ConfigureAwait(false) };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP tool {ToolName} failed while handling request.", _tool.Name);
            return new CallToolResult
            {
                IsError = true,
                Content = { new TextContentBlock { Text = $"Tool '{_tool.Name}' failed: {ex.Message}" } }
            };
        }
    }

    private JsonObject? ConvertArguments(IReadOnlyDictionary<string, JsonElement>? arguments)
    {
        if (arguments is null || arguments.Count == 0) return null;
        var result = new JsonObject();
        foreach (var (key, element) in arguments)
        {
            try { result[key] = JsonNode.Parse(element.GetRawText()); }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse argument '{ArgumentKey}' for tool {ToolName}.", key, _tool.Name);
                result[key] = JsonValue.Create(element.GetRawText());
            }
        }
        return result;
    }
}
