namespace Localtunnel.Http
{
    public interface IHttpRequestProcessor
    {
        void Process(HttpTunnelConnectionContext connectionContext, ref HttpRequest httpRequest);
    }
}
