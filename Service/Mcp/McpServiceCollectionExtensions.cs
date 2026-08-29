using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

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
        services.AddSingleton<IMcpTool>(sp => new DelegateMcpTool(name, description, inputSchema, (args, ct) => invokeAsync(sp, args, ct)));
        services.AddSingleton<McpServerTool>(sp => new LegacyMcpServerToolAdapter(
            sp.GetServices<IMcpTool>().Last(tool => tool.Name == name), sp.GetRequiredService<ILoggerFactory>()));
        return services;
    }

    private sealed class DelegateMcpTool(string name, string description, JsonNode? inputSchema,
        Func<JsonObject?, CancellationToken, Task<JsonNode?>> invokeAsync) : IMcpTool
    {
        public string Name { get; } = name;
        public string Description { get; } = description;
        public JsonNode? InputSchema { get; } = inputSchema;
        public Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken) => invokeAsync(arguments, cancellationToken);
    }
}
