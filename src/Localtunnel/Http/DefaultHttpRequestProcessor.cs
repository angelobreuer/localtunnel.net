namespace Localtunnel.Http
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Primitives;

    public sealed class DefaultHttpRequestProcessor : IHttpRequestProcessor
    {
        /// <summary>
        ///     Gets a shared instance of the <see cref="DefaultHttpRequestProcessor"/> class.
        /// </summary>
        /// <value>a shared instance of the <see cref="DefaultHttpRequestProcessor"/> class</value>
        public static DefaultHttpRequestProcessor Instance { get; } = new DefaultHttpRequestProcessor();

        /// <inheritdoc/>
        public void Process(HttpTunnelConnectionContext connectionContext, ref HttpRequest httpRequest)
        {
            var headers = new Dictionary<string, StringValues>(httpRequest.Headers, StringComparer.OrdinalIgnoreCase)
            {
                ["Host"] = connectionContext.Connection.Options.Host,
                ["X-Passthrough"] = "false",
            };

            httpRequest = httpRequest with { Headers = headers, };
        }
    }
}
