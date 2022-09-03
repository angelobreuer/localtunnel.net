namespace Localtunnel.Handlers.Kestrel;

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging.Abstractions;

public class TunnelTransport : IConnectionListener
{
    private readonly SocketConnectionContextFactory _socketConnectionContextFactory;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<ConnectionContext> _connections;
    private readonly SemaphoreSlim _connectionsLock;
    private readonly ITunnelAcceptorProvider _tunnelAcceptorProvider;

    public TunnelTransport(EndPoint endPoint, ITunnelAcceptorProvider tunnelAcceptorProvider)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(tunnelAcceptorProvider);

        EndPoint = endPoint;

        _tunnelAcceptorProvider = tunnelAcceptorProvider;
        _cancellationTokenSource = new CancellationTokenSource();
        _connections = new List<ConnectionContext>();
        _connectionsLock = new SemaphoreSlim(1, 1);

        var options = new SocketConnectionFactoryOptions { };
        var nullLogger = NullLogger<SocketConnectionContextFactory>.Instance;
        _socketConnectionContextFactory = new SocketConnectionContextFactory(options, nullLogger);
        _ = StartCleanUpTaskAsync(_cancellationTokenSource.Token);
    }

    /// <inheritdoc/>
    public EndPoint EndPoint { get; }

    /// <inheritdoc/>
    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        var acceptContext = await _tunnelAcceptorProvider.AcceptAsync(cancellationToken);
        var connectionContext = _socketConnectionContextFactory.Create(acceptContext.Socket);
        var tunnelConnectionContext = new KestrelTunnelConnectionContext(connectionContext, acceptContext.TunnelTraceListener);
        connectionContext.Features.Set(new TunnelTraceContextFeature(acceptContext.TunnelTraceListener.CreateContext(tunnelConnectionContext)));
        acceptContext.TaskCompletionSource.TrySetResult(tunnelConnectionContext);
        return connectionContext;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Dispose();
        return default;
    }

    /// <inheritdoc/>
    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        return default;
    }

    private async Task StartCleanUpTaskAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!cancellationToken.IsCancellationRequested)
        {
            // acquire lock
            await _connectionsLock.WaitAsync(cancellationToken);

            // ensure the lock is released
            try
            {
                _connections.RemoveAll(x => x.ConnectionClosed.IsCancellationRequested);
            }
            finally
            {
                // release lock
                _connectionsLock.Release();
            }

            await periodicTimer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
