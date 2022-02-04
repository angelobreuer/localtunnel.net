namespace Localtunnel.Http
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
    using Microsoft.Extensions.Primitives;

    public readonly record struct HttpRequest
    {
        public HttpVersion HttpVersion { get; init; }

        public string PathAndQuery { get; init; }

        public string RequestMethod { get; init; }

        public IReadOnlyDictionary<string, StringValues> Headers { get; init; }
    }
}
