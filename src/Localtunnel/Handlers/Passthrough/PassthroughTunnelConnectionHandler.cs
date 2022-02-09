namespace Localtunnel.Handlers.Passthrough;

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Endpoints;
using Localtunnel.Tunnels;

public sealed class PassthroughTunnelConnectionHandler : ITunnelConnectionHandler
{
    private readonly ITunnelEndpointFactory _tunnelEndpointFactory;

    public PassthroughTunnelConnectionHandler(ITunnelEndpointFactory tunnelEndpointFactory)
    {
        _tunnelEndpointFactory = tunnelEndpointFactory;
    }

    /// <inheritdoc/>
    public ValueTask<ITunnelConnectionContext> AcceptConnectionAsync(Socket socket, TunnelTraceListener tunnelTraceListener, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var transport = _tunnelEndpointFactory.Create();
        return ValueTask.FromResult<ITunnelConnectionContext>(new PassthroughTunnelConnectionContext(socket, transport, tunnelTraceListener));
    }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return default;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return default;
    }
}
