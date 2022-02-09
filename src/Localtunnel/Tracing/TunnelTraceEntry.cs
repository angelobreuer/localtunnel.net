namespace Localtunnel.Tracing;

using System;
using System.Net.Http;

public class TunnelTraceEntry
{
    public HttpRequestMessage RequestMessage { get; init; }

    public HttpResponseMessage ResponseMessage { get; private set; }

    public bool IsCompleted { get; private set; }

    internal void SetCompleted() => IsCompleted = true;

    internal void SetResponseMessage(HttpResponseMessage responseMessage)
    {
        if (ResponseMessage is not null && !ReferenceEquals(responseMessage, ResponseMessage))
        {
            throw new InvalidOperationException("Response message was set previously.");
        }

        ResponseMessage = responseMessage;
    }
}