namespace Localtunnel.Processors;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IHttpRequestProcessingPipeline
{
    ValueTask HandleRequestAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
