namespace Localtunnel.Connections;

using System;
using System.Buffers;
using System.Net.Sockets;
using Localtunnel.Tunnels;

public abstract class TunnelConnection : IDisposable
{
    private readonly TunnelSocketContext _socketContext;

    protected TunnelConnection(TunnelConnectionHandle handle)
    {
        _socketContext = handle.SocketContext ?? throw new InvalidOperationException("Invalid tunnel handle.");
    }

    public Socket? Socket => _socketContext.Socket;

    public Tunnel Tunnel => _socketContext.Tunnel;

    internal bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected internal abstract void Open();

    /// <summary>
    ///     Processes data received from the tunnel.
    /// </summary>
    /// <remarks>
    ///     If this method throws an exception, the caller is always responsible for returning
    ///     the buffer to the array pool.
    /// </remarks>
    /// <param name="arrayPool">
    ///     the array pool the buffer containing the received data was rent from.
    /// </param>
    /// <param name="data">the data that was received.</param>
    /// <returns>
    ///     a value indicating whether the received data was fully processed, if <see
    ///     langword="false"/> then the buffer will not be returned to the array pool (the
    ///     callee is fully responsible for returning the buffer to the array pool), if <see
    ///     langword="true"/> then the buffer will be returned to the array pool.
    /// </returns>
    protected internal abstract bool Process(ArrayPool<byte> arrayPool, ArraySegment<byte> data);

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;

        if (disposing)
        {
            if (ReferenceEquals(_socketContext.Connection, this))
            {
                _socketContext.BeginConnect();
            }
        }
    }
}
