namespace Localtunnel.Processors;
using System.Collections.Generic;
using Localtunnel.Handlers.Kestrel;

public sealed class HttpRequestProcessingPipelineBuilder
{
    private readonly List<IRequestProcessor> _requestProcessors = new();

    public HttpRequestProcessingPipelineBuilder Append(IRequestProcessor requestProcessor)
    {
        _requestProcessors.Add(requestProcessor);
        return this;
    }

    public IHttpRequestProcessingPipeline Build()
    {
        var stack = new Stack<HttpRequestProcessingPipelineItem>();
        var previousChainItem = default(HttpRequestProcessingPipelineItem?);

        for (int index = _requestProcessors.Count - 1; index >= 0; index--)
        {
            var processor = _requestProcessors[index];
            var item = new HttpRequestProcessingPipelineItem(processor, previousChainItem);
            stack.Push(previousChainItem = item);
        }

        return new BuiltHttpRequestProcessingPipeline(previousChainItem!);
    }
}
