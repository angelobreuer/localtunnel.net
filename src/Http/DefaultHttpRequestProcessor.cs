namespace Localtunnel.Http
{
    using System.Net.Http;
    using Localtunnel.Connections;

    public sealed class DefaultHttpRequestProcessor : IHttpRequestProcessor
    {
        /// <summary>
        ///     Gets a shared instance of the <see cref="DefaultHttpRequestProcessor"/> class.
        /// </summary>
        /// <value>a shared instance of the <see cref="DefaultHttpRequestProcessor"/> class</value>
        public static DefaultHttpRequestProcessor Instance { get; } = new DefaultHttpRequestProcessor();

        /// <inheritdoc/>
        public void Process(ProxiedHttpTunnelConnection connection, HttpRequestMessage requestMessage)
        {
            // change host
            requestMessage.Headers.Host = connection.Options.Host;
        }
    }
}
