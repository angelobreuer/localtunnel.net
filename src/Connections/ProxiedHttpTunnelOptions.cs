namespace Localtunnel.Connections
{
    using Localtunnel.Http;

    public class ProxiedHttpTunnelOptions
    {
        public string Host { get; set; } = "localhost";

        public int Port { get; set; } = 80;

        public int ReceiveBufferSize { get; set; } = 64 * 1024;

        public IHttpRequestProcessor? RequestProcessor { get; set; } = DefaultHttpRequestProcessor.Instance;
    }
}
