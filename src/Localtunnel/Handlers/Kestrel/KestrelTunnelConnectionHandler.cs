namespace Localtunnel.Handlers.Kestrel;

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Localtunnel.Endpoints;
using Localtunnel.Processors;
using Localtunnel.Tunnels;

public class KestrelTunnelConnectionHandler : ITunnelConnectionHandler
{
    private readonly Channel<KestrelTunnelConnectionAcceptContext> _acceptQueue;
    private readonly KestrelHttpServerContext _kestrelHttpServerContext;

    public KestrelTunnelConnectionHandler(IHttpRequestProcessingPipeline httpRequestProcessingPipeline, ITunnelEndpointFactory tunnelEndpointFactory)
    {
        _acceptQueue = Channel.CreateUnbounded<KestrelTunnelConnectionAcceptContext>();

        var tunnelAcceptorProvider = new TunnelAcceptorProvider(_acceptQueue.Reader);
        _kestrelHttpServerContext = new KestrelHttpServerContext(tunnelAcceptorProvider, tunnelEndpointFactory, httpRequestProcessingPipeline);
    }

    /// <inheritdoc/>
    public async ValueTask<ITunnelConnectionContext> AcceptConnectionAsync(Socket socket, TunnelTraceListener tunnelTraceListener, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // wait for zero byte read
        await socket
            .ReceiveAsync(Array.Empty<byte>(), SocketFlags.None)
            .ConfigureAwait(false);

        var acceptContext = new KestrelTunnelConnectionAcceptContext(socket, tunnelTraceListener);
        _acceptQueue.Writer.TryWrite(acceptContext);
        return await acceptContext.TaskCompletionSource.Task;
    }

    /// <inheritdoc/>
    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _kestrelHttpServerContext
            .StartAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _kestrelHttpServerContext
            .StopAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}