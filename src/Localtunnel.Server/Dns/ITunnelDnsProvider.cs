namespace Localtunnel.Server;

public interface ITunnelDnsProvider
{
    bool TryGetTunnel(HostString hostString, string? subdomainName, out TunnelId tunnelId);

    bool TryGetTunnel(HostString hostString, out TunnelId tunnelId);
}