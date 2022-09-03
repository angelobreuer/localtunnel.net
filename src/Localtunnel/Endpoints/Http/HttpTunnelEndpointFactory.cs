namespace Localtunnel.Endpoints.Http;

using System.Net;

public sealed class HttpTunnelEndpointFactory : ITunnelEndpointFactory
{
    private readonly EndPoint _endPoint;

    public HttpTunnelEndpointFactory(EndPoint endPoint)
    {
        _endPoint = endPoint;
    }

    public HttpTunnelEndpointFactory(IPAddress ipAddress, int port = 80)
    {
        _endPoint = new IPEndPoint(ipAddress, port);
    }

    public HttpTunnelEndpointFactory(string hostName, int port = 80)
    {
        _endPoint = new DnsEndPoint(hostName, port);
    }

    /// <inheritdoc/>
    public ITunnelEndpoint Create()
    {
        return new HttpTunnelEndpoint(_endPoint);
    }
}
