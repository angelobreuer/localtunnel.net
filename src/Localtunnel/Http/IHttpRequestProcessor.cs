namespace Localtunnel.Http;

public interface IHttpRequestProcessor
{
    void Process(IHttpTunnelConnectionContext connectionContext, ref HttpRequest httpRequest);
}
