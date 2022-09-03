namespace Localtunnel.Endpoints.Http;

using System.IO;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

internal sealed class HttpsTunnelEndpoint : HttpTunnelEndpoint
{
    public HttpsTunnelEndpoint(EndPoint endPoint)
        : base(endPoint)
    {
    }

    public HttpsTunnelEndpoint(IPAddress ipAddress, int port = 443)
        : base(ipAddress, port)
    {
    }

    public HttpsTunnelEndpoint(string hostName, int port = 443)
        : base(hostName, port)
    {
    }

    protected override async ValueTask<Stream> CreateStream(Stream stream, CancellationToken cancellationToken = default)
    {
        var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);

        var targetHost = EndPoint is DnsEndPoint dnsEndPoint
            ? dnsEndPoint.Host
            : ((IPEndPoint)EndPoint!).Address.ToString();

        var sslClientAuthenticationOptions = new SslClientAuthenticationOptions { TargetHost = targetHost, };

        await sslStream.AuthenticateAsClientAsync(sslClientAuthenticationOptions, cancellationToken);
        return sslStream;
    }
}
