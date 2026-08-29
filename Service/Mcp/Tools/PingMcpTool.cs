using System.Text.Json.Nodes;

namespace NORCE.Drilling.Rig.Service.Mcp.Tools;

public sealed class PingMcpTool : IMcpTool
{
    public string Name => "ping";
    public string Description => "Returns a pong response so clients can verify MCP connectivity.";
    public JsonNode? InputSchema => null;
    public Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken) =>
        Task.FromResult<JsonNode?>(new JsonObject { ["message"] = "pong", ["timestamp"] = DateTimeOffset.UtcNow.ToString("O") });
}
