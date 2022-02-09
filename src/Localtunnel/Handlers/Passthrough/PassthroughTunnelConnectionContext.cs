namespace Localtunnel.Handlers.Passthrough;

using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Connections;
using Localtunnel.Endpoints;
using Localtunnel.Tracing;
using Localtunnel.Tunnels;

internal sealed class PassthroughTunnelConnectionContext : ITunnelConnectionContext
{
    private readonly Socket _socket;
    private readonly ITunnelEndpoint _endpoint;
    private readonly TunnelConnectionTraceContext _connectionTraceContext;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private long _bytesIn;
    private long _bytesOut;

    public PassthroughTunnelConnectionContext(Socket socket, ITunnelEndpoint endpoint, TunnelTraceListener traceListener)
    {
        _socket = socket;
        _endpoint = endpoint;
        _connectionTraceContext = traceListener.CreateContext(this);
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <inheritdoc/>
    public IPEndPoint RemoteEndPoint => (IPEndPoint)_socket.RemoteEndPoint!;

    /// <inheritdoc/>
    public IPEndPoint LocalEndPoint => (IPEndPoint)_socket.LocalEndPoint!;

    /// <inheritdoc/>
    public HttpRequestMessage? RequestMessage => null; // never available when passing through request

    /// <inheritdoc/>
    public HttpResponseMessage? ResponseMessage => null; // never available when passing through request

    public bool IsOpen { get; private set; }

    public ConnectionStatistics Statistics => new(_bytesIn, _bytesOut);

    /// <inheritdoc/>
    public void Abort()
    {
        _cancellationTokenSource.Cancel();
    }

    /// <inheritdoc/>
    public async ValueTask RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IsOpen = true;

        try
        {
            _connectionTraceContext.OnConnectionStarted();

            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                token1: cancellationToken,
                token2: _cancellationTokenSource.Token);

            cancellationToken = linkedCancellationTokenSource.Token;

            await using var endpointStream = await _endpoint
                .CreateStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            await using var networkStream = new NetworkStream(_socket, ownsSocket: true);

            var remoteToEndpointCopyTask = CopyToWithStatisticsAsync(
                sourceStream: networkStream,
                destinationStream: endpointStream,
                statisticsValueReference: () => ref _bytesIn,
                cancellationToken: cancellationToken);

            var endpointToRemoteCopyTask = CopyToWithStatisticsAsync(
                sourceStream: endpointStream,
                destinationStream: networkStream,
                statisticsValueReference: () => ref _bytesOut,
                cancellationToken: cancellationToken);

            await Task
                .WhenAll(remoteToEndpointCopyTask, endpointToRemoteCopyTask)
                .ConfigureAwait(false);
        }
        finally
        {
            _connectionTraceContext.OnConnectionCompleted();
            IsOpen = false;
        }
    }

    public delegate ref long StatisticsValueReference();

    private async Task CopyToWithStatisticsAsync(Stream sourceStream, Stream destinationStream, StatisticsValueReference statisticsValueReference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pooledBuffer = ArrayPool<byte>.Shared.Rent(64 * 1024);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await sourceStream
                    .ReadAsync(pooledBuffer.AsMemory(), cancellationToken)
                    .ConfigureAwait(false);

                await destinationStream
                    .WriteAsync(pooledBuffer.AsMemory(0, bytesRead), cancellationToken)
                    .ConfigureAwait(false);

                Interlocked.Add(ref statisticsValueReference(), bytesRead);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pooledBuffer);
        }
    }
}