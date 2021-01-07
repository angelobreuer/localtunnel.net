namespace Localtunnel.Connections
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Net.Http;
    using System.Net.Sockets;
    using Localtunnel.Http;

    public class ProxiedHttpTunnelConnection : TunnelConnection
    {
        private readonly byte[] _receiveBuffer;
        private bool _disposed;
        private bool _httpInformationParsed;
        private Socket? _proxySocket;
        private Stream? _proxyStream;
        private ConnectionStatistics _statistics;

        public ProxiedHttpTunnelConnection(TunnelConnectionHandle handle, ProxiedHttpTunnelOptions options)
            : base(handle)
        {
            _receiveBuffer = ArrayPool<byte>.Shared.Rent(options.ReceiveBufferSize);
            Options = options ?? throw new ArgumentNullException(nameof(options));
            BaseUri = GetBaseUri();
        }

        public Uri BaseUri { get; }

        public ProxiedHttpTunnelOptions Options { get; }

        public HttpRequestMessage? RequestMessage { get; private set; }

        public ConnectionStatistics Statistics => _statistics;

        /// <inheritdoc/>
        protected internal override void Open()
        {
            _proxySocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _proxySocket.Connect(Options.Host, Options.Port);
            _proxyStream = CreateProxyStream(_proxySocket);
            BeginRead();
        }

        /// <inheritdoc/>
        protected internal override bool Process(ArrayPool<byte> arrayPool, ArraySegment<byte> data)
        {
            if (!_httpInformationParsed && Options.RequestProcessor is not null)
            {
                _httpInformationParsed = true;
                ProcessRequest(ref data);
            }

            _statistics.BytesIn += data.Count;
            _proxyStream!.Write(data);
            return true;
        }

        protected virtual Stream CreateProxyStream(Socket proxySocket) => new NetworkStream(proxySocket);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

            RequestMessage?.Dispose();
            _proxyStream?.Dispose();
            _proxySocket?.Dispose();
            ArrayPool<byte>.Shared.Return(_receiveBuffer);
        }

        protected virtual Uri GetBaseUri()
        {
            return new UriBuilder(Uri.UriSchemeHttp, Options.Host, Options.Port).Uri;
        }

        private static void ReceiveCallback(IAsyncResult asyncResult)
        {
            var instance = (ProxiedHttpTunnelConnection)asyncResult.AsyncState!;
            instance.ReceiveCallbackInternal(asyncResult);
        }

        private void BeginRead()
        {
            _proxyStream!.BeginRead(
                buffer: _receiveBuffer,
                offset: 0,
                count: _receiveBuffer.Length,
                callback: ReceiveCallback,
                state: this);
        }

        private void ProcessRequest(ref ArraySegment<byte> data)
        {
            var memoryStream = new MemoryStream(data.Array!, data.Offset, data.Array!.Length);

            using (var streamReader = new StreamReader(memoryStream, leaveOpen: true))
            {
                RequestMessage = RequestReader.Parse(streamReader, BaseUri)!;
            }

            // save request body as span
            var requestBody = data.Array.AsSpan(data.Offset + (int)memoryStream.Position);
            memoryStream.Position = 0;

            Options.RequestProcessor!.Process(this, RequestMessage);

            // write request back
            using (var streamWriter = new StreamWriter(memoryStream, leaveOpen: true))
            {
                RequestWriter.WriteRequest(streamWriter, RequestMessage);
            }

            // write request body
            memoryStream.Write(requestBody);
            data = new(data.Array!, data.Offset, (int)memoryStream.Length);
        }

        private void ReceiveCallbackInternal(IAsyncResult asyncResult)
        {
            if (!_proxySocket!.Connected)
            {
                Dispose();
                return;
            }

            try
            {
                var length = _proxyStream!.EndRead(asyncResult);

                if (length is 0)
                {
                    Dispose();
                    return;
                }

                _statistics.BytesOut += length;
                Socket.Send(_receiveBuffer, 0, length, SocketFlags.None);
                BeginRead();
            }
            catch (Exception)
            {
                Dispose();
            }
        }
    }
}
