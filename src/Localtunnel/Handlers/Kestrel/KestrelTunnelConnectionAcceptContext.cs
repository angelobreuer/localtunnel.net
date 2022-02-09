namespace Localtunnel.Handlers.Kestrel;

using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Localtunnel.Tunnels;

public sealed class KestrelTunnelConnectionAcceptContext
{
    public KestrelTunnelConnectionAcceptContext(Socket socket, TunnelTraceListener tunnelTraceListener)
    {
        Socket = socket ?? throw new ArgumentNullException(nameof(socket));
        TunnelTraceListener = tunnelTraceListener;
        TaskCompletionSource = new TaskCompletionSource<ITunnelConnectionContext>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Socket Socket { get; }
    public TunnelTraceListener TunnelTraceListener { get; }
    public TaskCompletionSource<ITunnelConnectionContext> TaskCompletionSource { get; }
}
