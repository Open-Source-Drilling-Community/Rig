using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using OSDC.Drilling.Rig.Model;
using OSDC.Drilling.Rig.Service.Mcp.Tools;

namespace OSDC.Drilling.Rig.Service.Mcp;

public static class McpServiceCollectionExtensions
{
    public static IServiceCollection AddLegacyMcpTool<TTool>(this IServiceCollection services) where TTool : class, IMcpTool
    {
        services.AddSingleton<TTool>();
        services.AddSingleton<IMcpTool>(sp => sp.GetRequiredService<TTool>());
        services.AddSingleton<McpServerTool>(sp => new LegacyMcpServerToolAdapter(sp.GetRequiredService<TTool>(), sp.GetRequiredService<ILoggerFactory>()));
        return services;
    }

    public static IServiceCollection AddLegacyMcpTool(this IServiceCollection services, string name, string description, JsonNode? inputSchema,
        Func<IServiceProvider, JsonObject?, CancellationToken, Task<JsonNode?>> invokeAsync)
    {
        services.AddSingleton<IMcpTool>(sp => new DelegateMcpTool(name, description, inputSchema ?? McpToolArgumentHelpers.CreateEmptySchema(),
            OutputSchema(name), Behavior(name), (args, ct) => invokeAsync(sp, args, ct)));
        services.AddSingleton<McpServerTool>(sp => new LegacyMcpServerToolAdapter(
            sp.GetServices<IMcpTool>().Last(tool => tool.Name == name), sp.GetRequiredService<ILoggerFactory>()));
        return services;
    }

    private sealed class DelegateMcpTool(string name, string description, JsonNode inputSchema, JsonNode outputSchema, McpToolBehavior behavior,
        Func<JsonObject?, CancellationToken, Task<JsonNode?>> invokeAsync) : IMcpTool
    {
        public string Name { get; } = name;
        public string Description { get; } = description;
        public JsonNode InputSchema { get; } = inputSchema;
        public JsonNode OutputSchema { get; } = outputSchema;
        public McpToolBehavior Behavior { get; } = behavior;
        public Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken) => invokeAsync(arguments, cancellationToken);
    }

    private static JsonNode OutputSchema(string name) => name switch
    {
        "rig_get_all_ids" or "rig_feature_category_get_all_ids" => McpToolArgumentHelpers.CreateResourceOutputSchema(typeof(Guid), true),
        "rig_get_all_meta_info" or "rig_feature_category_get_all_meta_info" => McpToolArgumentHelpers.CreateResourceOutputSchema(typeof(OSDC.DotnetLibraries.General.DataManagement.MetaInfo), true),
        "rig_get_by_id" => McpToolArgumentHelpers.CreateResourceOutputSchema(typeof(RigReadResponse)),
        "rig_get_all" => McpToolArgumentHelpers.CreateResourceOutputSchema(typeof(RigReadResponse), true),
        "rig_get_all_light" => McpToolArgumentHelpers.CreateResourceOutputSchema(typeof(RigLight), true),
        "rig_update_by_id" => McpToolArgumentHelpers.CreateResourceOutputSchema(typeof(Model.Rig)),
        "rig_batch_export" => McpToolArgumentHelpers.CreateResourceOutputSchema(typeof(RigBatchExportDocument)),
        "rig_batch_restore" => McpToolArgumentHelpers.CreateResourceOutputSchema(typeof(RigBatchRestoreResponse)),
        "rig_feature_category_get_all" => McpToolArgumentHelpers.CreateResourceOutputSchema(typeof(RigFeatureCategory), true),
        "rig_feature_category_get_by_id" or "rig_feature_category_create" or "rig_feature_category_update_by_id" => McpToolArgumentHelpers.CreateResourceOutputSchema(typeof(RigFeatureCategory)),
        _ => McpToolArgumentHelpers.CreateStatusOutputSchema()
    };

    private static McpToolBehavior Behavior(string name)
    {
        bool read = name.StartsWith("rig_get_", StringComparison.Ordinal) || name.StartsWith("rig_feature_category_get_", StringComparison.Ordinal) || name == "rig_batch_export";
        bool delete = name.Contains("delete", StringComparison.Ordinal);
        bool update = name.Contains("update", StringComparison.Ordinal);
        bool restore = name == "rig_batch_restore";
        string title = string.Join(' ', name.Split('_').Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        return new McpToolBehavior(title, read, delete || update || restore, read || delete || update, name is "rig_batch_export" or "rig_batch_restore");
    }
}
