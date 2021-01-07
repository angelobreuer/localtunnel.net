namespace Localtunnel.Tunnels
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using Localtunnel.Connections;
    using Localtunnel.Properties;
    using Microsoft.Extensions.Logging;

    internal sealed class TunnelSocketContext
    {
        private readonly string _label;
        private TunnelConnection? _connection;

        public TunnelSocketContext(Tunnel tunnel, IPEndPoint endPoint, string label)
        {
            _label = label ?? throw new ArgumentNullException(nameof(label));
            SocketAsyncEventArgs = new TunnelSocketAsyncEventArgs(this);
            Tunnel = tunnel ?? throw new ArgumentNullException(nameof(tunnel));
            SocketAsyncEventArgs.RemoteEndPoint = endPoint;
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

        public Socket Socket { get; private set; }

        public SocketAsyncEventArgs SocketAsyncEventArgs { get; }

        public Tunnel Tunnel { get; }

        public void BeginConnect()
        {
            if (_connection is not null)
            {
                Tunnel.Logger.LogDebug(Resources.ConnectionClosed, _label);

                _connection.Dispose();
                _connection = null;
                return; // connection will call BeginConnect()
            }

            // TODO implement socket reuse
            Socket?.Disconnect(reuseSocket: false);
            Socket?.Dispose();
            Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            if (!Socket.ConnectAsync(SocketAsyncEventArgs))
            {
                NotifyCompletion(SocketAsyncEventArgs);
            }
        }

        public void BeginReceive()
        {
            var buffer = Tunnel.ArrayPool.Rent(Tunnel.Information.ReceiveBufferSize);
            SocketAsyncEventArgs.SetBuffer(buffer, offset: 0, count: buffer.Length);

            if (!Socket.ReceiveAsync(SocketAsyncEventArgs))
            {
                NotifyCompletion(SocketAsyncEventArgs);
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
                // initialize connection
                var handle = new TunnelConnectionHandle(this);
                connection = _connection = Tunnel.ConnectionFactory(handle);
                connection.Open();
            }

            // capture buffer
            var buffer = new ArraySegment<byte>(eventArgs.Buffer, 0, eventArgs.BytesTransferred);

            var returnBuffer = true;
            try
            {
                returnBuffer = connection.Process(Tunnel.ArrayPool, buffer);
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
