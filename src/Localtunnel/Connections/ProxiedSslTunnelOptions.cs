namespace Localtunnel.Connections;

public sealed class ProxiedSslTunnelOptions : ProxiedHttpTunnelOptions
{
    public bool AllowUntrustedCertificates { get; set; }
}
