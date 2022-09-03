namespace Localtunnel.Connections;

using System.Threading;

internal sealed class TunnelLifetime
{
    private readonly CancellationTokenSource _cancellationTokenSource;

    public TunnelLifetime()
    {
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public CancellationToken TunnelRunning => _cancellationTokenSource.Token;

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }
}
