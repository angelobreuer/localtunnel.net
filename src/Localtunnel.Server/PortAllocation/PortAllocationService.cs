namespace Localtunnel.Server;

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

public sealed class PortAllocationService : IPortAllocationService
{
    private readonly IPAddress _bindAddress;

    public PortAllocationService(IOptions<PortAllocationServiceOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _bindAddress = InterpretIpAddress(options.Value.BindAddress);
    }

    public bool TryAllocateSocket(int maximumConnectionCount, [MaybeNullWhen(false)] out Socket serverSocket)
    {
        serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        try
        {
            serverSocket.Bind(new IPEndPoint(_bindAddress, 0));
            serverSocket.Listen(maximumConnectionCount);
        }
        catch (Exception)
        {
            serverSocket.Dispose();
            return false;
        }

        return true;
    }

    private static IPAddress InterpretIpAddress(string ipAddress) => ipAddress switch
    {
        "Any" => Socket.OSSupportsIPv6 ? IPAddress.IPv6Any : IPAddress.Any,
        "IPv6Any" => IPAddress.IPv6Any,
        "IPv4Any" => IPAddress.Any,
        _ => IPAddress.Parse(ipAddress),
    };

    private record struct PortRange(IPAddress IpAddress, IEnumerable<int> Ports);
}
