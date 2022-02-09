namespace Localtunnel.Endpoints.Http;

using System.Net;

public sealed class HttpsTunnelEndpointFactory : ITunnelEndpointFactory
{
    private readonly EndPoint _endPoint;

    public HttpsTunnelEndpointFactory(EndPoint endPoint)
    {
        _endPoint = endPoint;
    }

    public HttpsTunnelEndpointFactory(IPAddress ipAddress, int port = 443)
    {
        _endPoint = new IPEndPoint(ipAddress, port);
    }

    public HttpsTunnelEndpointFactory(string hostName, int port = 443)
    {
        _endPoint = new DnsEndPoint(hostName, port);
    }

    /// <inheritdoc/>
    public ITunnelEndpoint Create()
    {
        return new HttpsTunnelEndpoint(_endPoint);
    }
}
