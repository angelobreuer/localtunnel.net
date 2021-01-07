namespace Localtunnel.Http
{
    using System.Net.Http;
    using Localtunnel.Connections;

    public interface IHttpRequestProcessor
    {
        void Process(ProxiedHttpTunnelConnection connection, HttpRequestMessage requestMessage);
    }
}
