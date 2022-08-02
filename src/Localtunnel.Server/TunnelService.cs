namespace Localtunnel.Server;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

internal sealed class TunnelService : ITunnelService
{
    private readonly ConcurrentDictionary<TunnelId, TunnelInformation> _tunnel;
    private readonly IPortAllocationService _portAllocationService;
    private readonly ILogger<TunnelService> _logger;

    public TunnelService(IPortAllocationService portAllocationService, ILogger<TunnelService> logger)
    {
        ArgumentNullException.ThrowIfNull(portAllocationService);
        ArgumentNullException.ThrowIfNull(logger);

        _tunnel = new ConcurrentDictionary<TunnelId, TunnelInformation>();
        _portAllocationService = portAllocationService;
        _logger = logger;
    }

    public TunnelInformation GetTunnel(TunnelId id)
    {
        return _tunnel.GetOrAdd(id, CreateTunnelInternal);
    }

    internal bool TryUnregister(TunnelId id)
    {
        _logger.LogInformation("Tunnel '{Id}' was unregistered.", id);
        return _tunnel.Remove(id, out _);
    }

    public bool TryGetTunnel(TunnelId id, [MaybeNullWhen(false)] out TunnelInformation tunnelInformation)
    {
        return _tunnel.TryGetValue(id, out tunnelInformation);
    }

    private TunnelInformation CreateTunnelInternal(TunnelId id)
    {
        _logger.LogInformation("Tunnel '{Id}' was created.", id);

        if (!_portAllocationService.TryAllocateSocket(10, out var serverSocket))
        {
            throw new InvalidOperationException("Unable to allocate server socket.");
        }

        return new TunnelInformation(id, serverSocket, this);
    }
}