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
        _protocolTool = new Tool
        {
            Name = tool.Name, Title = tool.Behavior.Title, Description = tool.Description,
            InputSchema = JsonSerializer.SerializeToElement(tool.InputSchema, SerializerOptions),
            OutputSchema = JsonSerializer.SerializeToElement(tool.OutputSchema, SerializerOptions),
            Annotations = new()
            {
                Title = tool.Behavior.Title, ReadOnlyHint = tool.Behavior.ReadOnlyHint,
                DestructiveHint = tool.Behavior.DestructiveHint, IdempotentHint = tool.Behavior.IdempotentHint,
                OpenWorldHint = tool.Behavior.OpenWorldHint
            }
        };
    }

    public override Tool ProtocolTool => _protocolTool;
    public override IReadOnlyList<object> Metadata { get; } = Array.Empty<object>();

    public override async ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            JsonNode? result = await _tool.InvokeAsync(ConvertArguments(request.Params?.Arguments), cancellationToken).ConfigureAwait(false);
            if (TryGetFailure(result, out JsonNode failure)) return Error(failure);
            string? fallback = result?.ToJsonString(SerializerOptions);
            return new CallToolResult
            {
                StructuredContent = result?.DeepClone(),
                Content = fallback is null ? [] : [new TextContentBlock { Text = fallback }]
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP tool {ToolName} failed while handling request.", _tool.Name);
            return Error(new JsonObject { ["error"] = "internal_error", ["message"] = "An unexpected server error occurred while executing the tool.", ["errors"] = new JsonArray() });
        }
    }

    private static bool TryGetFailure(JsonNode? result, out JsonNode failure)
    {
        failure = null!;
        if (result is not JsonObject response || response["status"]?.GetValue<int>() is not int status || status < 400) return false;
        failure = response["data"] is JsonNode payload ? NormalizeFailure(payload, status) :
            new JsonObject { ["error"] = ErrorCodeForStatus(status), ["message"] = "The tool request failed.", ["errors"] = new JsonArray() };
        return true;
    }

    private static JsonNode NormalizeFailure(JsonNode payload, int status)
    {
        if (payload is not JsonObject source)
            return new JsonObject { ["error"] = ErrorCodeForStatus(status), ["message"] = payload.ToJsonString(SerializerOptions), ["errors"] = new JsonArray() };
        JsonArray normalized = [];
        if ((source["errors"] ?? source["Errors"]) is JsonArray errors)
            foreach (JsonNode? item in errors)
                if (item is JsonObject detail)
                    normalized.Add(new JsonObject
                    {
                        ["positionIndex"] = (detail["positionIndex"] ?? detail["PositionIndex"])?.DeepClone(),
                        ["property"] = (detail["property"] ?? detail["Property"])?.DeepClone(),
                        ["code"] = (detail["code"] ?? detail["Code"])?.DeepClone(),
                        ["message"] = (detail["message"] ?? detail["Message"])?.DeepClone()
                    });
                else if (item is JsonValue value && value.TryGetValue(out string? message))
                    normalized.Add(new JsonObject
                    {
                        ["property"] = "request", ["code"] = "invalid_value", ["message"] = message
                    });
        return new JsonObject
        {
            ["error"] = (source["error"] ?? source["Error"])?.DeepClone() ?? JsonValue.Create(ErrorCodeForStatus(status)),
            ["message"] = (source["message"] ?? source["Message"])?.DeepClone() ?? JsonValue.Create("The tool request failed."),
            ["errors"] = normalized
        };
    }

    private static string ErrorCodeForStatus(int status) => status switch
    { 400 => "validation_failed", 404 => "not_found", 409 => "conflict", 502 => "dependency_unavailable", _ => "request_failed" };

    private static CallToolResult Error(JsonNode problem) => new()
    { IsError = true, Content = { new TextContentBlock { Text = problem.ToJsonString(SerializerOptions) } } };

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
