namespace Localtunnel.Tunnels
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Localtunnel.Connections;
    using Localtunnel.Properties;
    using Microsoft.Extensions.Logging;

    internal sealed class TunnelSocketContext : IDisposable
    {
        private readonly string _label;
        private TunnelConnection? _connection;
        private bool _disposed;

        public TunnelSocketContext(Tunnel tunnel, IPEndPoint endPoint, string label)
        {
            _label = label ?? throw new ArgumentNullException(nameof(label));
            Tunnel = tunnel ?? throw new ArgumentNullException(nameof(tunnel));
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }

        public TunnelConnection? Connection
        {
            get
            {
                if (_connection is null)
                {
                    return null;
                }

                if (_connection.IsDisposed)
                {
                    return _connection = null;
                }

                return _connection;
            }
        }

        public IPEndPoint EndPoint { get; }

        public Socket? Socket { get; private set; }

        public SocketAsyncEventArgs? SocketAsyncEventArgs { get; private set; }

        public Tunnel Tunnel { get; }

        public void BeginConnect()
        {
            CloseConnection();

            SocketAsyncEventArgs = new TunnelSocketAsyncEventArgs(this) { RemoteEndPoint = EndPoint, };

            if (Socket is not null)
            {
                CloseSocket();
            }

            Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            if (!Socket.ConnectAsync(SocketAsyncEventArgs))
            {
                NotifyCompletion(SocketAsyncEventArgs);
            }
        }

        public void BeginReceive()
        {
            var buffer = Tunnel.ArrayPool.Rent(Tunnel.Information.ReceiveBufferSize);
            SocketAsyncEventArgs!.SetBuffer(buffer, offset: 0, count: buffer.Length);

            if (!Socket!.ReceiveAsync(SocketAsyncEventArgs))
            {
                NotifyCompletion(SocketAsyncEventArgs);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void CloseConnection()
        {
            var connection = Interlocked.Exchange(ref _connection, null);

            if (connection is not null && !connection.IsDisposed)
            {
                Tunnel.Logger.LogDebug(Resources.ConnectionClosed, _label);
                connection.Dispose();
            }
        }

        private void CloseSocket()
        {
            if (Socket is null)
            {
                return;
            }

            if (Socket.Connected)
            {
                try
                {
                    Socket.Disconnect(reuseSocket: false);
                }
                catch (Exception)
                {
                }
            }

            Socket.Dispose();
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (disposing)
            {
                SocketAsyncEventArgs?.Dispose();
                Socket?.Dispose();

                CloseConnection();
            }
        }

        private void NotifyCompletedReceive(SocketAsyncEventArgs eventArgs)
        {
            var connection = Connection;

            if (eventArgs.BytesTransferred is 0)
            {
                BeginConnect();
                return;
            }

            if (connection is null)
            {
                Tunnel.Logger?.LogDebug("[{0}] Got incoming proxy client, creating connection...", _label);

                // initialize connection
                var handle = new TunnelConnectionHandle(this);
                connection = _connection = Tunnel.ConnectionFactory(handle);

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    Dispose();
                    return;
                }
            }

            // capture buffer
            var buffer = new ArraySegment<byte>(eventArgs.Buffer!, 0, eventArgs.BytesTransferred);

            var returnBuffer = true;
            try
            {
                returnBuffer = connection.Process(Tunnel.ArrayPool, buffer);
            }
            catch (Exception)
            {
                Dispose();
                return;
            }
            finally
            {
                if (returnBuffer)
                {
                    Tunnel.ArrayPool.Return(buffer.Array!);
                }
            }

            if (Socket is not null && Socket.Connected)
            {
                // next receive
                BeginReceive();
            }
        }

        private void NotifyCompletion(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs.SocketError is not SocketError.Success)
            {
                Tunnel.Logger?.LogDebug(
                    Resources.ConnectionError, _label,
                    eventArgs.SocketError, eventArgs.LastOperation);

                BeginConnect();
                return;
            }

            if (eventArgs.LastOperation is SocketAsyncOperation.Connect)
            {
                Tunnel.Logger?.LogDebug(
                    "[{0}] Connected to server, waiting for incoming proxy client.", _label);

                BeginReceive();
            }
            else if (eventArgs.LastOperation is SocketAsyncOperation.Receive)
            {
                NotifyCompletedReceive(eventArgs);
            }
        }

        private sealed class TunnelSocketAsyncEventArgs : SocketAsyncEventArgs
        {
            public TunnelSocketAsyncEventArgs(TunnelSocketContext socketContext)
            {
                SocketContext = socketContext ?? throw new ArgumentNullException(nameof(socketContext));
            }

            public TunnelSocketContext SocketContext { get; }

            /// <inheritdoc/>
            protected override void OnCompleted(SocketAsyncEventArgs eventArgs)
            {
                base.OnCompleted(eventArgs);
                SocketContext.NotifyCompletion(eventArgs);
            }
        }
    }
}
