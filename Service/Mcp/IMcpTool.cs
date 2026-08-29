using System.Text.Json.Nodes;

namespace NORCE.Drilling.Rig.Service.Mcp;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    JsonNode? InputSchema { get; }
    Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken);
}
