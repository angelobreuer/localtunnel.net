namespace Localtunnel.Server;

using System.Diagnostics.CodeAnalysis;

internal interface ITunnelService
{
    TunnelInformation GetTunnel(TunnelId id);

    bool TryGetTunnel(TunnelId id, [MaybeNullWhen(false)] out TunnelInformation tunnelInformation);
}