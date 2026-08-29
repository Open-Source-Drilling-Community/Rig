using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace OSDC.Drilling.Rig.Service.Mcp;

public static class McpWebSocketEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapMcpWebSocket(this IEndpointRouteBuilder endpoints, string pattern = "/mcp/ws") =>
        endpoints.MapGet(pattern, HandleAsync).WithName("McpWebSocket");

    private static async Task HandleAsync(HttpContext context)
    {
        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("MCP.WebSocket");
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Expected a WebSocket request.");
            return;
        }
        var httpOptions = context.RequestServices.GetService<IOptions<HttpServerTransportOptions>>();
        if (httpOptions?.Value.Stateless == true)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket transport is unavailable in stateless mode.");
            return;
        }
        var serverOptions = context.RequestServices.GetRequiredService<IOptionsFactory<McpServerOptions>>().Create(Options.DefaultName);
        var cancellationToken = context.RequestAborted;
        if (httpOptions?.Value.ConfigureSessionOptions is { } configure)
            await configure(context, serverOptions, cancellationToken);
        var handshake = McpHandshakeReader.FromHttpRequest(context.Request);
        if (!string.IsNullOrWhiteSpace(handshake.ClientName) && !string.IsNullOrWhiteSpace(handshake.ClientVersion))
            serverOptions.KnownClientInfo = new Implementation { Name = handshake.ClientName!, Version = handshake.ClientVersion! };
        WebSocket socket;
        try { socket = await context.WebSockets.AcceptWebSocketAsync(); }
        catch (Exception ex) { logger.LogError(ex, "Failed to accept MCP WebSocket connection."); return; }
        await using var transport = new WebSocketServerTransport(socket, "websocket", loggerFactory, handshake.SessionId);
        try
        {
            await using var server = McpServer.Create(transport, serverOptions, loggerFactory, context.RequestServices);
            context.Features.Set(server);
            if (httpOptions?.Value.RunSessionHandler is { } handler) await handler(context, server, cancellationToken).ConfigureAwait(false);
            else await server.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogError(ex, "Unhandled MCP WebSocket error."); }
        finally { context.Features.Set<McpServer?>(null); }
    }
}
