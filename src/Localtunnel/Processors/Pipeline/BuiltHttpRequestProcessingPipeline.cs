namespace Localtunnel.Processors;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

internal sealed class BuiltHttpRequestProcessingPipeline : IHttpRequestProcessingPipeline
{
    private readonly HttpRequestProcessingPipelineItem? _firstChainItem;

    public BuiltHttpRequestProcessingPipeline(HttpRequestProcessingPipelineItem? firstChainItem)
    {
        _firstChainItem = firstChainItem;
    }

    /// <inheritdoc/>
    public ValueTask HandleRequestAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_firstChainItem is null)
        {
            return ValueTask.CompletedTask;
        }

        return _firstChainItem.HandleRequestAsync(httpContext, cancellationToken);
    }
}
