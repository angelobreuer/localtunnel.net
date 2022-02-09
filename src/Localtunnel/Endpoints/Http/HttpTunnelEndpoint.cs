namespace Localtunnel.Endpoints.Http;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

internal class HttpTunnelEndpoint : ITunnelEndpoint
{
    public EndPoint EndPoint { get; }

    public HttpTunnelEndpoint(EndPoint endPoint)
    {
        EndPoint = endPoint;
    }

    public HttpTunnelEndpoint(IPAddress ipAddress, int port = 80)
    {
        EndPoint = new IPEndPoint(ipAddress, port);
    }

    public HttpTunnelEndpoint(string hostName, int port = 80)
    {
        EndPoint = new DnsEndPoint(hostName, port);
    }

    protected virtual ValueTask<Stream> CreateStream(Stream stream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(stream);
    }

    /// <inheritdoc/>
    public async ValueTask<Stream> CreateStreamAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(EndPoint, cancellationToken).ConfigureAwait(false);
        return await CreateStream(new NetworkStream(socket, ownsSocket: true), cancellationToken);
    }
}
