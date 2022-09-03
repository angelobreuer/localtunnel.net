namespace Localtunnel.Handlers.Kestrel;

using System.Threading;
using System.Threading.Tasks;

public interface ITunnelAcceptorProvider
{
    ValueTask<KestrelTunnelConnectionAcceptContext> AcceptAsync(CancellationToken cancellationToken = default);
}
