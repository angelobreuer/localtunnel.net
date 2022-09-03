namespace Localtunnel.Handlers;

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Connections;

public interface ITunnelConnectionContext
{
    bool IsOpen { get; }

    IPEndPoint RemoteEndPoint { get; }

    IPEndPoint LocalEndPoint { get; }

    ConnectionStatistics Statistics { get; }

    HttpRequestMessage? RequestMessage { get; }

    HttpResponseMessage? ResponseMessage { get; }

    void Abort();

    ValueTask RunAsync(CancellationToken cancellationToken = default);
}
