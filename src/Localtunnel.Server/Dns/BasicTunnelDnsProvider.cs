namespace Localtunnel.Server;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

internal sealed class BasicTunnelDnsProvider : ITunnelDnsProvider
{
    private readonly string _zone;

    public BasicTunnelDnsProvider(IOptions<BasicTunnelDnsProviderOptions> options!!)
    {
        _zone = options.Value.Zone ?? throw new InvalidOperationException("The DNS zone was not set.");
    }

    public bool TryGetTunnel(HostString hostString, out TunnelId id)
    {
        id = default;

        if (hostString.Host[0] == '[') // IPv6
        {
            return false;
        }

        if (hostString.Host.Length < _zone.Length + 2 /* dot + min 1 char */)
        {
            return false;
        }

        if (!hostString.Host.EndsWith(_zone, StringComparison.OrdinalIgnoreCase))
        {
            // does not end with DNS zone
            return false;
        }

        if (hostString.Host[^(_zone.Length + 1)] is not '.')
        {
            // no separator
            return false;
        }

        id = new TunnelId(_zone, hostString.Host[..^(_zone.Length + 1)]);
        return true;
    }

    /// <inheritdoc/>
    public bool TryGetTunnel(HostString hostString, string? subdomainName, out TunnelId tunnelId)
    {
        if (!hostString.Value.Equals(_zone, StringComparison.OrdinalIgnoreCase))
        {
            tunnelId = default;
            return false;
        }

        var id = subdomainName ?? Guid.NewGuid().ToString("N");
        tunnelId = new TunnelId(_zone, id);
        return true;
    }
}