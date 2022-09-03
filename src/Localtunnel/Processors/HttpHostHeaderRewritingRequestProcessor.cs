namespace Localtunnel.Processors;

using System;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Handlers.Kestrel;
using Microsoft.AspNetCore.Http;

public sealed class HttpHostHeaderRewritingRequestProcessor : IRequestProcessor
{
    private readonly string _host;

    public HttpHostHeaderRewritingRequestProcessor(string host)
    {
        _host = host;
    }

    public ValueTask HandleRequestAsync(HttpContext httpContext, Func<ValueTask> next, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        httpContext.Request.Headers["Host"] = _host;
        return next();
    }
}
