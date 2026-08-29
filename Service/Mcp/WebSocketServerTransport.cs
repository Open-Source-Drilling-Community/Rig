using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace OSDC.Drilling.Rig.Service.Mcp;

internal sealed class WebSocketServerTransport : TransportBase
{
    private readonly WebSocket _socket;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ILogger<WebSocketServerTransport> _logger;
    private readonly Task _receiveLoop;

    public WebSocketServerTransport(WebSocket socket, string endpointName, ILoggerFactory? loggerFactory, string? sessionId) : base(endpointName, loggerFactory)
    {
        _socket = socket;
        SessionId = sessionId;
        _logger = loggerFactory?.CreateLogger<WebSocketServerTransport>() ?? NullLogger<WebSocketServerTransport>.Instance;
        SetConnected();
        _receiveLoop = Task.Run(ReceiveMessagesAsync);
    }

    public override async Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected) throw new InvalidOperationException("Transport is not connected.");
        var payload = JsonSerializer.SerializeToUtf8Bytes(message, McpJsonUtilities.DefaultOptions);
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { await _socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false); }
        finally { _sendLock.Release(); }
    }

    public override async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        await _receiveLoop.ConfigureAwait(false);
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try { await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closing connection.", CancellationToken.None).ConfigureAwait(false); }
            catch (Exception ex) { _logger.LogDebug(ex, "Failed to close MCP WebSocket gracefully."); }
        }
        _sendLock.Dispose();
        _shutdown.Dispose();
        _socket.Dispose();
        SetDisconnected();
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[16 * 1024];
        await using var stream = new MemoryStream();
        try
        {
            while (!_shutdown.IsCancellationRequested && _socket.State == WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(buffer, _shutdown.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close) break;
                stream.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage) continue;
                var bytes = stream.ToArray();
                stream.SetLength(0);
                try
                {
                    var message = JsonSerializer.Deserialize<JsonRpcMessage>(bytes, McpJsonUtilities.DefaultOptions);
                    if (message is not null) await WriteMessageAsync(message, _shutdown.Token).ConfigureAwait(false);
                }
                catch (JsonException ex) { _logger.LogWarning(ex, "Invalid MCP WebSocket payload: {Payload}", Encoding.UTF8.GetString(bytes)); }
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { }
        catch (Exception ex) { _logger.LogError(ex, "Unexpected MCP WebSocket receive error."); }
        finally { SetDisconnected(); }
    }
}
