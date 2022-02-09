namespace Localtunnel.Handlers.Kestrel;

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Connections;
using Localtunnel.Tracing;
using Localtunnel.Tunnels;
using Microsoft.AspNetCore.Connections;

public class KestrelTunnelConnectionContext : ITunnelConnectionContext
{
    private readonly ConnectionContext _connectionContext;
    private readonly TunnelConnectionTraceContext _connectionTraceContext;

    public KestrelTunnelConnectionContext(ConnectionContext connectionContext!!, TunnelTraceListener traceListener)
    {
        _connectionContext = connectionContext;
        _connectionTraceContext = traceListener.CreateContext(this);
    }

    /// <inheritdoc/>
    public IPEndPoint RemoteEndPoint => (IPEndPoint)_connectionContext.RemoteEndPoint!;

    /// <inheritdoc/>
    public IPEndPoint LocalEndPoint => (IPEndPoint)_connectionContext.LocalEndPoint!;

    /// <inheritdoc/>
    public HttpRequestMessage? RequestMessage { get; internal set; }

    /// <inheritdoc/>
    public HttpResponseMessage? ResponseMessage { get; internal set; }

    public bool IsOpen { get; private set; }

    public ConnectionStatistics Statistics { get; internal set; }

    /// <inheritdoc/>
    public void Abort()
    {
        _connectionContext.Abort();
    }

    /// <inheritdoc/>
    public async ValueTask RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IsOpen = true;

        try
        {
            _connectionTraceContext.OnConnectionStarted();

            var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var cancellationTokenRegistration = cancellationToken.CanBeCanceled
                ? cancellationToken.Register(static state => ((TaskCompletionSource)state!).TrySetResult(), taskCompletionSource)
                : default;

            var connectionClosedTokenRegistration = _connectionContext.ConnectionClosed.CanBeCanceled
                ? _connectionContext.ConnectionClosed.Register(static state => ((TaskCompletionSource)state!).TrySetResult(), taskCompletionSource)
                : default;

            await taskCompletionSource.Task.ConfigureAwait(false);
        }
        finally
        {
            IsOpen = false;
            _connectionTraceContext.OnConnectionCompleted();
        }
    }
}
