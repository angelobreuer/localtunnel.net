namespace Localtunnel.Handlers;

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Tunnels;

public interface ITunnelConnectionHandler
{
    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);

    ValueTask<ITunnelConnectionContext> AcceptConnectionAsync(Socket socket, TunnelTraceListener tunnelTraceListener, CancellationToken cancellationToken = default);
}
