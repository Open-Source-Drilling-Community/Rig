using System.Text.Json.Nodes;

namespace OSDC.Drilling.Rig.Service.Mcp.Tools;

internal static class McpToolResponses
{
    public static JsonNode CreateValidationError(string message) => new JsonObject
    {
        ["status"] = 400,
        ["data"] = new JsonObject
        {
            ["error"] = "validation_failed",
            ["message"] = "The tool arguments are invalid.",
            ["errors"] = new JsonArray(new JsonObject
            {
                ["property"] = "arguments", ["code"] = "invalid_argument", ["message"] = message
            })
        }
    };
}
