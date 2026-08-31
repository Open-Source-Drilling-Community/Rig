using System.Text.Json.Nodes;

namespace OSDC.Drilling.Rig.Service.Mcp.Tools;

public sealed class PingMcpTool : IMcpTool
{
    public string Name => "ping";
    public string Description => "Returns a pong response so clients can verify MCP connectivity.";
    public McpToolBehavior Behavior => new("Ping Rig MCP", true, false, true, false);
    public JsonNode InputSchema => JsonNode.Parse("""{"type":"object","additionalProperties":false}""")!;
    public JsonNode OutputSchema => JsonNode.Parse("""{"type":"object","properties":{"message":{"type":"string"},"timestamp":{"type":"string","format":"date-time"}},"required":["message","timestamp"],"additionalProperties":false}""")!;
    public Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken) =>
        Task.FromResult<JsonNode?>(new JsonObject { ["message"] = "pong", ["timestamp"] = DateTimeOffset.UtcNow.ToString("O") });
}
