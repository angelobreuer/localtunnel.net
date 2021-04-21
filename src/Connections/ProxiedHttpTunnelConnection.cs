namespace Localtunnel.Connections
{
    using System;
    using System.Buffers;
    using System.Collections.Specialized;
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

        public HttpRequestMessage? HttpRequest { get; private set; }

        public NameValueCollection? ContentHeaders { get; private set; }

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
            var requestBuffer = data.Array!;
            var requestBody = (ReadOnlySpan<byte>)data;

            var ret = RequestReader.Parse(ref requestBody, BaseUri)!;
            HttpRequest = ret.Item1;
            ContentHeaders = ret.Item2;

            Options.RequestProcessor!.Process(this, HttpRequest);

            var pooledBuffer = Tunnel.ArrayPool.Rent(data.Count + 8096);

            // write request back
            int requestLength;
            using (var memoryStream = new MemoryStream(pooledBuffer))
            {
                using (var streamWriter = new StreamWriter(memoryStream, leaveOpen: true))
                {
                    RequestWriter.WriteRequest(streamWriter, HttpRequest, requestBody.Length, ContentHeaders);
                }

                requestLength = (int)memoryStream.Position;
            }

            requestBody.CopyTo(pooledBuffer.AsSpan(requestLength));
            data = new(pooledBuffer, 0, requestLength + requestBody.Length);

            // return current buffer
            Tunnel.ArrayPool.Return(requestBuffer);
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
