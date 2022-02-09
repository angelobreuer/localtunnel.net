namespace Localtunnel.Handlers.Kestrel;

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

public class TunnelTransportFactory : IConnectionListenerFactory
{
    private readonly ITunnelAcceptorProvider _tunnelAcceptorProvider;

    public TunnelTransportFactory(ITunnelAcceptorProvider tunnelAcceptorProvider)
    {
        _tunnelAcceptorProvider = tunnelAcceptorProvider;
    }

    /// <inheritdoc/>
    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IConnectionListener>(new TunnelTransport(endpoint, _tunnelAcceptorProvider));
    }
}
