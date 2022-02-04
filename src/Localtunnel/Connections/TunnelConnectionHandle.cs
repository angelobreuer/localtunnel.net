namespace Localtunnel.Connections;

using System;
using Localtunnel.Tunnels;

public readonly struct TunnelConnectionHandle
{
    internal TunnelConnectionHandle(TunnelSocketContext socketContext)
    {
        SocketContext = socketContext ?? throw new ArgumentNullException(nameof(socketContext));
    }

    internal TunnelSocketContext SocketContext { get; }
}
