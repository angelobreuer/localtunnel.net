namespace Localtunnel.Handlers.Kestrel;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IRequestProcessor
{
    ValueTask HandleRequestAsync(HttpContext httpContext, Func<ValueTask> next, CancellationToken cancellationToken = default);
}
