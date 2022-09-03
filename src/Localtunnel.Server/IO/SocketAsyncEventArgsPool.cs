namespace Localtunnel.Server;

using Microsoft.Extensions.ObjectPool;

internal static class SocketAsyncEventArgsPool
{
    public static ObjectPool<AcceptSocketAsyncEventArgs> AcceptPool { get; } = ObjectPool.Create<AcceptSocketAsyncEventArgs>();
}