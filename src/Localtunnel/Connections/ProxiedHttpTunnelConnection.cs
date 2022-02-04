namespace Localtunnel.Connections;

using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Localtunnel.Http;

public class ProxiedHttpTunnelConnection : TunnelConnection
{
    private readonly byte[] _receiveBuffer;
    private bool _disposed;
    private Socket? _proxySocket;
    private Stream? _proxyStream;
    private ConnectionStatistics _statistics;
    private int _synchronousStackDepth;

    public ProxiedHttpTunnelConnection(TunnelConnectionHandle handle, ProxiedHttpTunnelOptions options)
        : base(handle)
    {
        _receiveBuffer = ArrayPool<byte>.Shared.Rent(options.ReceiveBufferSize);
        Options = options ?? throw new ArgumentNullException(nameof(options));
        BaseUri = GetBaseUri();
    }

    public ProxiedHttpTunnelOptions Options { get; }
    public Uri BaseUri { get; }
    public HttpRequest? HttpRequest => _httpConnectionContext?.HttpRequest;

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
    protected internal override bool Process(ArrayPool<byte>? arrayPool, ArraySegment<byte> data)
    {
        try
        {
            var memory = data.AsMemory();

            if (Options.RequestProcessor is not null)
            {
                ProcessRequest(ref memory);
            }

            _statistics.BytesIn += memory.Length;
            _proxyStream!.Write(memory.Span);
        }
        finally
        {
            arrayPool?.Return(data.Array!);
        }

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

    private void ProcessRequest(ref Memory<byte> data)
    {
        _httpConnectionContext ??= new HttpTunnelConnectionContext(this);

        if (!_httpConnectionContext.ProcessData(data))
        {
            return;
        }

        data = _httpConnectionContext.Buffer.ToArray();

    }
    private HttpTunnelConnectionContext _httpConnectionContext = null;

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
            Socket?.Send(_receiveBuffer, 0, length, SocketFlags.None);

            if (!asyncResult.CompletedSynchronously)
            {
                _synchronousStackDepth = 0;
                BeginRead();
            }
            else if (_synchronousStackDepth++ > 100)
            {
                ThreadPool.QueueUserWorkItem(
                    callBack: static state => ((ProxiedHttpTunnelConnection)state!).BeginRead(),
                    state: this);

                _synchronousStackDepth = 0;
            }
            else
            {
                BeginRead();
            }
        }
        catch (Exception)
        {
            Dispose();
        }
    }
}
