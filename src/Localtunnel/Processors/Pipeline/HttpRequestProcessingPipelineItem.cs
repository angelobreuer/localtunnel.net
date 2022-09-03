namespace Localtunnel.Processors;

using System;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Handlers.Kestrel;
using Microsoft.AspNetCore.Http;

internal class HttpRequestProcessingPipelineItem
{
    private readonly IRequestProcessor _processorItem;
    private readonly HttpRequestProcessingPipelineItem? _nextChainItem;

    public HttpRequestProcessingPipelineItem(IRequestProcessor processorItem, HttpRequestProcessingPipelineItem? nextChainItem)
    {
        _processorItem = processorItem;
        _nextChainItem = nextChainItem;
    }

    public async ValueTask HandleRequestAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var next = _nextChainItem is null
            ? (Func<ValueTask>)(() => ValueTask.CompletedTask)
            : () => HandleNextRequestAsync(httpContext, cancellationToken);

        await _processorItem
            .HandleRequestAsync(httpContext, next, cancellationToken)
            .ConfigureAwait(false);
    }

    private ValueTask HandleNextRequestAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _nextChainItem.HandleRequestAsync(httpContext, cancellationToken);
    }
}
