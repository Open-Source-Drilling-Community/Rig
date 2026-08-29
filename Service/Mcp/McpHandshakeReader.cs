using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace NORCE.Drilling.Rig.Service.Mcp;

internal static class McpHandshakeReader
{
    public static McpHandshake FromHttpRequest(HttpRequest request)
    {
        JsonObject? capabilities = null;
        if (request.Headers.TryGetValue("X-MCP-Capabilities", out var values))
        {
            try { capabilities = JsonNode.Parse(values.FirstOrDefault() ?? "") as JsonObject; } catch (JsonException) { }
        }
        return new McpHandshake(Extract(request, "protocolVersion", "X-MCP-Protocol-Version") ?? "0.1",
            Extract(request, "client", "X-MCP-Client-Name"), Extract(request, "clientVersion", "X-MCP-Client-Version"),
            Extract(request, "sessionId", "X-MCP-Session-Id"), capabilities);
    }

    private static string? Extract(HttpRequest request, string queryKey, string headerKey)
    {
        var value = request.Query.TryGetValue(queryKey, out var query) ? query.FirstOrDefault() : null;
        return !string.IsNullOrWhiteSpace(value) ? value : request.Headers.TryGetValue(headerKey, out var header) ? header.FirstOrDefault() : null;
    }
}
