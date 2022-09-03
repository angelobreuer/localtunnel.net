namespace Localtunnel.Tunnels;
internal sealed class NullTunnelTraceListener : TunnelTraceListener
{
    private static NullTunnelTraceListener? _instance;

    private NullTunnelTraceListener()
    {
    }

    /// <summary>
    ///     Gets a shared instance of the <see cref="NullTunnelTraceListener"/> class.
    /// </summary>
    /// <value>a shared instance of the <see cref="NullTunnelTraceListener"/> class</value>
    public static NullTunnelTraceListener Instance => _instance ??= new NullTunnelTraceListener();
}
