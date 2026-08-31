using System.Text.Json.Nodes;

namespace OSDC.Drilling.Rig.Service.Mcp;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    McpToolBehavior Behavior { get; }
    JsonNode InputSchema { get; }
    JsonNode OutputSchema { get; }
    Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken);
}

public sealed record McpToolBehavior(string Title, bool ReadOnlyHint, bool DestructiveHint,
    bool IdempotentHint, bool OpenWorldHint = false);
