using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Rig.Service.Controllers;
using OSDC.Drilling.Rig.Service.Managers;
using RigModel = OSDC.Drilling.Rig.Model.Rig;

namespace OSDC.Drilling.Rig.Service.Mcp.Tools;

public static class RigRestMcpToolRegistrations
{
    public static IServiceCollection AddRigRestMcpTools(this IServiceCollection services)
    {
        services.AddLegacyMcpTool("rig_get_all_ids", "List the UUIDs of every stored rig. Use this compact discovery operation when only identifiers are required, then pass one UUID to rig_get_by_id to retrieve the complete rig, mast, and installed-equipment configuration.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => Controller(sp).GetAllRigId()));
        services.AddLegacyMcpTool("rig_get_all_meta_info", "List MetaInfo for every stored rig without loading the large equipment payloads. Each result contains the persistent UUID and may contain HTTP location metadata; use its ID with rig_get_by_id for the complete configuration.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => Controller(sp).GetAllRigMetaInfo()));
        services.AddLegacyMcpTool("rig_get_by_id", "Retrieve one complete rig by UUID, including platform and Cluster association, drill-floor elevation, main and auxiliary mast assemblies, pumps, tanks, solids-control equipment, MPD/BOP equipment, and all nested ratings and measurements. Numeric physical values use SI units.", McpToolArgumentHelpers.CreateGuidSchema("id", "UUID of the rig to retrieve."),
            (sp, args, ct) => InvokeById(args, ct, id => Controller(sp).GetRigById(id)));
        services.AddLegacyMcpTool("rig_get_all_light", "List lightweight rig records containing metadata, name, description, timestamps, fixed-platform status, and optional Cluster UUID. Use this for listing, sorting, and selection; it deliberately omits mast and equipment payloads.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => Controller(sp).GetAllRigLight()));
        services.AddLegacyMcpTool("rig_get_all", "Retrieve every rig with its complete nested mast and equipment configuration. This can produce a very large response, so prefer rig_get_all_ids, rig_get_all_meta_info, or rig_get_all_light for discovery and rig_get_by_id for one selected rig. Physical values use SI units.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => Controller(sp).GetAllRig()));
        services.AddLegacyMcpTool("rig_create", "Persist a new complete rig configuration. Generate a non-empty rig.MetaInfo.ID first; an existing UUID produces a conflict. For a fixed platform set IsFixedPlatform true and ClusterID to an existing Cluster UUID; otherwise leave ClusterID null. Send nested equipment objects and all physical values in SI units.", McpToolArgumentHelpers.CreateRigSchema(),
            (sp, args, ct) => InvokeWithBody<RigModel>(args, "rig", ct, data => Controller(sp).PostRig(data)));
        services.AddLegacyMcpTool("rig_update_by_id", "Replace an existing rig configuration. The path id must exactly match rig.MetaInfo.ID or the request is rejected. Send the complete desired representation because omitted equipment may be lost, update LastModificationDate, preserve the fixed-platform/Cluster relationship, and use SI physical values.", McpToolArgumentHelpers.CreateRigSchema(includeId: true),
            (sp, args, ct) => InvokeWithIdAndBody<RigModel>(args, "rig", ct, (id, data) => Controller(sp).PutRigById(id, data)));
        services.AddLegacyMcpTool("rig_delete_by_id", "Permanently delete the stored rig identified by UUID. Use a read operation first when the target is uncertain. The operation returns not found when the UUID is unknown; it accepts a Rig resource UUID, not a Cluster UUID or equipment identifier.", McpToolArgumentHelpers.CreateGuidSchema("id", "UUID of the rig to delete."),
            (sp, args, ct) => InvokeDelete(args, ct, id => Controller(sp).DeleteRigById(id)));
        return services;
    }

    private static Task<JsonNode?> Invoke<T>(CancellationToken ct, Func<ActionResult<T>> action)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action()));
    }

    private static Task<JsonNode?> InvokeById<T>(JsonObject? args, CancellationToken ct, Func<Guid, ActionResult<T>> action)
    {
        ct.ThrowIfCancellationRequested();
        return McpToolArgumentHelpers.TryParseGuid(args, "id", out var id, out var error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id)))
            : Task.FromResult(error);
    }

    private static Task<JsonNode?> InvokeDelete(JsonObject? args, CancellationToken ct, Func<Guid, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        return McpToolArgumentHelpers.TryParseGuid(args, "id", out var id, out var error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id)))
            : Task.FromResult(error);
    }

    private static Task<JsonNode?> InvokeWithBody<T>(JsonObject? args, string bodyName, CancellationToken ct, Func<T?, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        return TryDeserialize(args, bodyName, out T? data, out var error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(data)))
            : Task.FromResult(error);
    }

    private static Task<JsonNode?> InvokeWithIdAndBody<T>(JsonObject? args, string bodyName, CancellationToken ct, Func<Guid, T?, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(args, "id", out var id, out var idError)) return Task.FromResult(idError);
        return TryDeserialize(args, bodyName, out T? data, out var error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id, data)))
            : Task.FromResult(error);
    }

    private static bool TryDeserialize<T>(JsonObject? args, string bodyName, out T? data, out JsonNode? error)
    {
        data = default;
        error = null;
        if (args?[bodyName] is not JsonNode node)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{bodyName}' is required.");
            return false;
        }
        try
        {
            data = node.Deserialize<T>(JsonSettings.Options);
            if (data is null) throw new InvalidOperationException();
            return true;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{bodyName}' could not be deserialized.");
            return false;
        }
    }

    private static RigController Controller(IServiceProvider sp) => new(
        sp.GetRequiredService<ILogger<RigManager>>(),
        sp.GetRequiredService<SqlConnectionManager>());
}
