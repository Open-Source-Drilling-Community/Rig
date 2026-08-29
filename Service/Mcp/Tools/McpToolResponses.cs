using System.Text.Json.Nodes;

namespace NORCE.Drilling.Rig.Service.Mcp.Tools;

internal static class McpToolResponses
{
    public static JsonNode CreateValidationError(string message) => new JsonObject { ["status"] = 400, ["error"] = message };
}
