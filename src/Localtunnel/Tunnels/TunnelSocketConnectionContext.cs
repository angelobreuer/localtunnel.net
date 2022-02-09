namespace Localtunnel.Tunnels;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Connections;
using Microsoft.Extensions.Logging;

internal sealed class TunnelSocketConnectionContext : IDisposable
{
    private readonly Tunnel _tunnel;
    private readonly string _contextIdentifier;
    private readonly IPEndPoint _targetEndpoint;
    private readonly ILogger<TunnelSocketConnectionContext> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;

    public TunnelSocketConnectionContext(Tunnel tunnel, string contextIdentifier, IPEndPoint targetEndpoint, TunnelLifetime tunnelLifetime, ILogger<TunnelSocketConnectionContext> logger)
    {
        _tunnel = tunnel;
        _contextIdentifier = contextIdentifier;
        _targetEndpoint = targetEndpoint;
        _logger = logger;

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(tunnelLifetime.TunnelRunning);
        _ = RunAsync(_cancellationTokenSource.Token);
    }

    public Socket? Socket { get; private set; }

    private async Task RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("[{Identifier}] Started new connection to upstream.", _contextIdentifier);

            try
            {
                using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                Socket = socket;

                await socket
                    .ConnectAsync(_targetEndpoint, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation("[{Identifier}] Waiting for request.", _contextIdentifier);

                await _tunnel
                    .AcceptSocketAsync(socket, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
            }
            finally
            {
                Socket?.Dispose();
                Socket = null;
                _logger.LogInformation("[{Identifier}] Reconnecting context.", _contextIdentifier);
            }
        }

        _logger.LogInformation("[{Identifier}] Closed context.", _contextIdentifier);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
