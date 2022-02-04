namespace Localtunnel.Server;

using System.Diagnostics;
using System.Net.Sockets;

internal sealed class AcceptSocketAsyncEventArgs : SocketAsyncEventArgs
{
    public TunnelInformation? TunnelInformation { get; set; }

    /// <inheritdoc/>
    protected override void OnCompleted(SocketAsyncEventArgs eventArgs)
    {
        base.OnCompleted(eventArgs);

        Debug.Assert(TunnelInformation is not null);
        TunnelInformation.OnAccepted(this);
    }
}