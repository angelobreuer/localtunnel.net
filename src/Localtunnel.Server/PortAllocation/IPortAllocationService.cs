namespace Localtunnel.Server;

using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

public interface IPortAllocationService
{
    bool TryAllocateSocket(int maximumConnectionCount, [MaybeNullWhen(false)] out Socket serverSocket);
}