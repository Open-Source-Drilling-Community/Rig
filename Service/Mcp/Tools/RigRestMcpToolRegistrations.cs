using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Rig.Service.Controllers;
using OSDC.Drilling.Rig.Service.Managers;
using OSDC.Drilling.Rig.Model;
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
        services.AddLegacyMcpTool("rig_get_by_id", "Retrieve one complete rig by UUID, including platform and Cluster association, drill-floor elevation, main and auxiliary mast assemblies, pumps, tanks, solids-control equipment, MPD/BOP equipment, ratings, limits, and instrumentation capabilities. The Rig contract contains no live telemetry. Numeric physical values use SI units. Photo metadata is opt-in through includePhotos; image bytes are never placed in MCP results.", McpToolArgumentHelpers.CreateRigReadSchema(includeId: true),
            (sp, args, ct) => InvokeById(args, ct, id => Controller(sp).GetRigById(id, ReadIncludePhotos(args))));
        services.AddLegacyMcpTool("rig_get_all_light", "List lightweight rig records containing metadata, name, description, timestamps, fixed-platform status, and optional Cluster UUID. Use this for listing, sorting, and selection; it deliberately omits mast and equipment payloads.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => Controller(sp).GetAllRigLight()));
        services.AddLegacyMcpTool("rig_get_all", "Retrieve every rig with its complete nested mast and equipment configuration, including static ratings, limits, and instrumentation capabilities but no live telemetry. This can produce a very large response, so prefer rig_get_all_ids, rig_get_all_meta_info, or rig_get_all_light for discovery and rig_get_by_id for one selected rig. Physical values use SI units. Photo metadata is excluded unless includePhotos is true; image bytes are never placed in MCP results.", McpToolArgumentHelpers.CreateRigReadSchema(includeId: false),
            (sp, args, ct) => Invoke(ct, () => Controller(sp).GetAllRig(ReadIncludePhotos(args))));
        services.AddLegacyMcpTool("rig_create", "Persist a new complete rig master-data configuration. Generate a non-empty rig.MetaInfo.ID first; an existing UUID produces a conflict. For a fixed platform set IsFixedPlatform true and ClusterID to an existing Cluster UUID; otherwise leave ClusterID null. Send static equipment specifications and limits in SI units; describe available signals through MeasurementCapabilities and do not send live telemetry.", McpToolArgumentHelpers.CreateRigSchema(),
            (sp, args, ct) => InvokeWithBody<RigModel>(args, "rig", ct, data => Controller(sp).PostRig(data)));
        services.AddLegacyMcpTool("rig_update_by_id", "Replace an existing rig master-data configuration using expectedModifiedUtc from the latest read for optimistic concurrency. The path id must exactly match rig.MetaInfo.ID or the request is rejected. Send the complete desired representation because omitted equipment is removed; the server assigns LastModificationDate. Preserve the fixed-platform/Cluster relationship and use SI physical values.", McpToolArgumentHelpers.CreateRigSchema(includeId: true),
            (sp, args, ct) => InvokeRigUpdate(sp, args, ct));
        services.AddLegacyMcpTool("rig_delete_by_id", "Permanently delete the stored rig identified by UUID. Use a read operation first when the target is uncertain. The operation returns not found when the UUID is unknown; it accepts a Rig resource UUID, not a Cluster UUID or equipment identifier.", McpToolArgumentHelpers.CreateGuidSchema("id", "UUID of the rig to delete."),
            (sp, args, ct) => InvokeDelete(args, ct, id => Controller(sp).DeleteRigById(id)));
        services.AddLegacyMcpTool("rig_batch_export", "Create a read-only, versioned JSON backup of every rig or an explicitly ordered selection. The result contains complete rig records, only referenced Rig Feature definitions and options, a live-verified Cluster UUID/name manifest, and attached photographs with Base64 content and SHA-256 metadata. This explicit backup operation can return a very large payload; ordinary rig reads never include image bytes. One invalid dependency rejects the complete export.", McpToolArgumentHelpers.CreateBatchExportSchema(),
            (sp, args, ct) => InvokeWithBodyResultAsync<RigBatchExportRequest, RigBatchExportDocument>(args, "request", ct,
                (request, token) => Controller(sp).BatchExportRigs(request, token)));
        services.AddLegacyMcpTool("rig_batch_restore", "Validate and atomically restore a versioned Rig backup. Feature definitions are mapped locally or created according to CatalogPolicy; Cluster references retain an existing UUID or map only through one unique normalized-name match. Rigs, catalog changes, and checksum-verified photographs commit together, while any ambiguity, collision, invalid image, or storage error commits nothing.", McpToolArgumentHelpers.CreateBatchRestoreSchema(),
            (sp, args, ct) => InvokeWithBodyResultAsync<RigBatchRestoreRequest, RigBatchRestoreResponse>(args, "request", ct,
                (request, token) => Controller(sp).BatchRestoreRigs(request, token)));
        services.AddLegacyMcpTool("rig_feature_category_get_all_ids", "List the UUIDs of all built-in and custom rig feature categories without returning their option catalogs. Use this compact discovery call when identifiers alone are sufficient, then retrieve a selected definition with rig_feature_category_get_by_id.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => FeatureController(sp).GetAllIds()));
        services.AddLegacyMcpTool("rig_feature_category_get_all_meta_info", "List the MetaInfo envelopes of all built-in and custom rig feature categories without returning option catalogs. Each result supplies the stable persistent UUID used by assignments and by rig_feature_category_get_by_id.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => FeatureController(sp).GetAllMetaInfo()));
        services.AddLegacyMcpTool("rig_feature_category_get_all", "List all built-in and custom rig feature categories and their selectable options, stable codes, descriptions, validity behavior, provenance, deprecation state, and timestamps. Built-ins use stable UUIDs shared by every deployment.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => FeatureController(sp).GetAll()));
        services.AddLegacyMcpTool("rig_feature_category_get_by_id", "Retrieve one rig feature category by UUID, including every option UUID, stable machine code, human description, validity behavior, provenance, deprecation state, and modification timestamp required for a concurrency-safe custom update.", McpToolArgumentHelpers.CreateGuidSchema("id", "UUID of the rig feature category."),
            (sp, args, ct) => InvokeById(args, ct, id => FeatureController(sp).GetById(id)));
        services.AddLegacyMcpTool("rig_feature_category_create", "Create a custom rig feature category and its initial option catalog. The server normalizes stable codes and generates the category UUID and every option UUID; callers cannot claim built-in provenance or modify the immutable predefined catalog.", McpToolArgumentHelpers.CreateFeatureCategorySchema(),
            (sp, args, ct) => InvokeFeatureCreate(sp, args, ct));
        services.AddLegacyMcpTool("rig_feature_category_update_by_id", "Replace one complete custom rig feature category using expectedModifiedUtc for optimistic concurrency. Built-in categories are immutable, new option UUIDs are generated by the server, and options still assigned to rigs cannot be removed.", McpToolArgumentHelpers.CreateFeatureCategorySchema(includeUpdateFields: true),
            (sp, args, ct) => InvokeFeatureUpdate(sp, args, ct));
        services.AddLegacyMcpTool("rig_feature_category_delete_by_id", "Permanently delete an unreferenced custom rig feature category. The service protects immutable built-ins and rejects deletion with conflict while any stored rig assignment references the category, preventing dangling catalog references.", McpToolArgumentHelpers.CreateGuidSchema("id", "UUID of the custom rig feature category to delete."),
            (sp, args, ct) => InvokeDelete(args, ct, id => FeatureController(sp).Delete(id)));
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

    private static bool ReadIncludePhotos(JsonObject? args) =>
        args?["includePhotos"]?.GetValue<bool>() ?? false;

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

    private static Task<JsonNode?> InvokeRigUpdate(IServiceProvider sp, JsonObject? args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(args, "id", out Guid id, out JsonNode? idError)) return Task.FromResult(idError);
        if (!TryDeserialize(args, "rig", out RigModel? rig, out JsonNode? rigError)) return Task.FromResult(rigError);
        if (!DateTimeOffset.TryParse(args?["expectedModifiedUtc"]?.GetValue<string>(), out DateTimeOffset expected))
            return Task.FromResult<JsonNode?>(McpToolResponses.CreateValidationError("Argument 'expectedModifiedUtc' must be an ISO 8601 timestamp."));
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(Controller(sp).PutRigById(id, expected, rig)));
    }

    private static async Task<JsonNode?> InvokeWithBodyResultAsync<TBody, TResult>(JsonObject? args, string bodyName,
        CancellationToken ct, Func<TBody?, CancellationToken, Task<ActionResult<TResult>>> action)
    {
        ct.ThrowIfCancellationRequested();
        if (!TryDeserialize(args, bodyName, out TBody? data, out JsonNode? error)) return error;
        return McpActionResultConverter.FromActionResult(await action(data, ct));
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

    private static Task<JsonNode?> InvokeFeatureCreate(IServiceProvider sp, JsonObject? args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!TryDeserialize(args, "category", out RigFeatureCategory? category, out JsonNode? error)) return Task.FromResult(error);
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(FeatureController(sp).Create(category)));
    }

    private static Task<JsonNode?> InvokeFeatureUpdate(IServiceProvider sp, JsonObject? args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(args, "id", out Guid id, out JsonNode? idError)) return Task.FromResult(idError);
        if (!TryDeserialize(args, "category", out RigFeatureCategory? category, out JsonNode? categoryError)) return Task.FromResult(categoryError);
        if (!DateTimeOffset.TryParse(args?["expectedModifiedUtc"]?.GetValue<string>(), out DateTimeOffset expected))
            return Task.FromResult<JsonNode?>(McpToolResponses.CreateValidationError("Argument 'expectedModifiedUtc' must be an ISO 8601 timestamp."));
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(FeatureController(sp).Update(id, expected, category)));
    }

    private static RigController Controller(IServiceProvider sp) => new(
        sp.GetRequiredService<ILogger<RigManager>>(),
        sp.GetRequiredService<SqlConnectionManager>(),
        sp.GetRequiredService<IRigExternalReferenceResolver>());

    private static RigFeatureCategoryController FeatureController(IServiceProvider sp) => new(
        sp.GetRequiredService<ILogger<RigFeatureCategoryManager>>(),
        sp.GetRequiredService<SqlConnectionManager>());
}
