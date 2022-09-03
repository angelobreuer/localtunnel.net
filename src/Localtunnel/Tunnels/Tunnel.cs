namespace Localtunnel.Tunnels;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Connections;
using Localtunnel.Handlers;
using Localtunnel.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class Tunnel : IDisposable
{
    private readonly LocaltunnelClient? _client;
    private readonly ITunnelConnectionHandler _tunnelConnectionHandler;
    private readonly TunnelSocketConnectionContext[] _socketContexts;
    private readonly TunnelLifetime _tunnelLifetime;
    private readonly ConcurrentDictionary<ITunnelConnectionContext, bool> _connections;
    private readonly ILogger<TunnelSocketConnectionContext> _tunnelSocketConnectionContextLogger;
    private bool _running;
    private bool _disposed;

    public Tunnel(
        LocaltunnelClient? client,
        TunnelInformation information,
        ITunnelConnectionHandler tunnelConnectionHandler,
        TunnelTraceListener tunnelTraceListener,
        ILoggerFactory? loggerFactory = null)
    {
        _client = client;
        Information = information;
        TunnelTraceListener = tunnelTraceListener;

        _tunnelSocketConnectionContextLogger = loggerFactory?.CreateLogger<TunnelSocketConnectionContext>() ?? NullLogger<TunnelSocketConnectionContext>.Instance;

        _connections = new ConcurrentDictionary<ITunnelConnectionContext, bool>();
        _tunnelConnectionHandler = tunnelConnectionHandler;
        _tunnelLifetime = new TunnelLifetime();
        _socketContexts = new TunnelSocketConnectionContext[10];

        _client?.TryRegister(this);
    }

    public IEnumerable<ITunnelConnectionContext> Connections => _connections.Keys;

    internal async ValueTask AcceptSocketAsync(Socket socket, CancellationToken cancellationToken = default)
    {
        var tunnelConnectionContext = await _tunnelConnectionHandler
            .AcceptConnectionAsync(socket, TunnelTraceListener, cancellationToken)
            .ConfigureAwait(false);

        _connections.TryAdd(tunnelConnectionContext, false);

        try
        {
            await tunnelConnectionContext
                .RunAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _connections.TryRemove(tunnelConnectionContext, out _);
        }
    }

    public TunnelInformation Information { get; }

    protected internal TunnelTraceListener TunnelTraceListener { get; }

    public async ValueTask StartAsync(int connections = 10, CancellationToken cancellationToken = default)
    {
        if (_running)
        {
            return;
        }

        _running = true;

        try
        {
            _client?.TryRegister(this);

            // perform DNS resolution once
            if (!IPAddress.TryParse(Information.Url.Host, out var ipAddress))
            {
                var ipHostEntry = await Dns.GetHostEntryAsync(Information.Url.DnsSafeHost);

                ipAddress = ipHostEntry.AddressList.FirstOrDefault()
                    ?? throw new Exception(string.Format(Resources.DnsResolutionFailed, Information.Url.DnsSafeHost));
            }

            await _tunnelConnectionHandler
                .StartAsync(cancellationToken)
                .ConfigureAwait(false);

            var endPoint = new IPEndPoint(ipAddress, Information.Port);

            for (var index = 0; index < Math.Min(connections, Information.MaximumConnections); index++)
            {
                _socketContexts[index] = new TunnelSocketConnectionContext(this, $"SocketContext-" + index, endPoint, _tunnelLifetime, _tunnelSocketConnectionContextLogger);
            }
        }
        catch
        {
            _running = false;
            throw;
        }
    }

    public void Stop()
    {
        if (!_running)
        {
            return;
        }

        _running = false;
        _tunnelLifetime.Stop();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            _client?.TryUnregister(this);
            Stop();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
