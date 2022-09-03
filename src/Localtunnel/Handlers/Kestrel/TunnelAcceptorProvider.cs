namespace Localtunnel.Handlers.Kestrel;

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

internal sealed class TunnelAcceptorProvider : ITunnelAcceptorProvider
{
    private readonly ChannelReader<KestrelTunnelConnectionAcceptContext> _channelReader;

    public TunnelAcceptorProvider(ChannelReader<KestrelTunnelConnectionAcceptContext> channelReader)
    {
        _channelReader = channelReader;
    }

    /// <inheritdoc/>
    public ValueTask<KestrelTunnelConnectionAcceptContext> AcceptAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _channelReader.ReadAsync(cancellationToken);
    }
}
