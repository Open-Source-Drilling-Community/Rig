using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OSDC.Drilling.Rig.Service.Mcp;

internal static class McpActionResultConverter
{
    public static JsonObject FromActionResult<T>(ActionResult<T> actionResult) => Build(actionResult.Result, actionResult.Value);
    public static JsonObject FromActionResult(ActionResult actionResult) => Build(actionResult, null);

    private static JsonObject Build(IActionResult? result, object? value)
    {
        var (status, payload) = Extract(result, value);
        var response = new JsonObject { ["status"] = status };
        if (payload is not null)
            response["data"] = payload is JsonNode node ? node.DeepClone() : JsonSerializer.SerializeToNode(payload, payload.GetType(), JsonSettings.Options);
        return response;
    }

    private static (int Status, object? Payload) Extract(IActionResult? result, object? value) => result switch
    {
        ObjectResult objectResult => (objectResult.StatusCode ?? StatusCodes.Status200OK, objectResult.Value ?? value),
        StatusCodeResult statusResult => (statusResult.StatusCode, value),
        null => (value is null ? StatusCodes.Status204NoContent : StatusCodes.Status200OK, value),
        EmptyResult => (StatusCodes.Status204NoContent, value),
        _ => (StatusCodes.Status200OK, value)
    };
}
