namespace Localtunnel.Tracing;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Localtunnel.Cli;
using Localtunnel.Handlers;
using Localtunnel.Tunnels;

public sealed class HistoryTraceListener : TunnelTraceListener
{
    private readonly ConcurrentQueue<RequestTraceEntry> _history;
    private readonly ConcurrentDictionary<ITunnelConnectionContext, RequestTraceEntry> _associationMap;
    private readonly int _maxHistorySize;

    public HistoryTraceListener(int maxHistorySize = 10)
    {
        _history = new ConcurrentQueue<RequestTraceEntry>();
        _maxHistorySize = maxHistorySize;
        _associationMap = new ConcurrentDictionary<ITunnelConnectionContext, RequestTraceEntry>();
        _history = new ConcurrentQueue<RequestTraceEntry>();
    }

    public event EventHandler? HistoryUpdated;

    public IEnumerable<RequestTraceEntry> Entries => _history.Take(_maxHistorySize); // need to limit because in high concurrency situations there may be more items returned than allowed

    protected internal override void OnConnectionStarted(TunnelConnectionTraceContext traceContext, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
    {
        base.OnConnectionStarted(traceContext, localEndPoint, remoteEndPoint);

        var traceEntry = new RequestTraceEntry(traceContext.ConnectionContext, localEndPoint, remoteEndPoint, default);
        _history.Enqueue(traceEntry);
        _associationMap[traceContext.ConnectionContext] = traceEntry;

        while (_history.Count > _maxHistorySize && _history.TryDequeue(out var entry))
        {
        }
    }

    protected internal override void OnConnectionCompleted(TunnelConnectionTraceContext traceContext)
    {
        base.OnConnectionCompleted(traceContext);

        _associationMap.TryRemove(traceContext.ConnectionContext, out _);
    }

    protected internal override void OnHttpRequestStarted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage)
    {
        base.OnHttpRequestStarted(traceContext, requestMessage);

        if (!_associationMap.TryGetValue(traceContext.ConnectionContext, out var entry))
        {
            return;
        }

        if (entry.RequestMessage is not null)
        {
            // new request
            entry = new RequestTraceEntry(
                connectionContext: traceContext.ConnectionContext,
                localEndPoint: traceContext.ConnectionContext.LocalEndPoint,
                remoteEndPoint: traceContext.ConnectionContext.RemoteEndPoint,
                statisticsOffset: traceContext.ConnectionContext.Statistics);

            _history.Enqueue(entry);
            _associationMap[traceContext.ConnectionContext] = entry;
        }

        entry.RequestMessage = requestMessage;
    }

    protected internal override void OnHttpRequestCompleted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage)
    {
        base.OnHttpRequestCompleted(traceContext, requestMessage);
    }

    protected internal override void OnHttpResponseCompleted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage, HttpResponseMessage responseMessage)
    {
        base.OnHttpResponseCompleted(traceContext, requestMessage, responseMessage);

        _associationMap.TryRemove(traceContext.ConnectionContext, out _);
    }

    protected internal override void OnHttpResponseStarted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage, HttpResponseMessage responseMessage)
    {
        base.OnHttpResponseStarted(traceContext, requestMessage, responseMessage);

        if (_associationMap.TryGetValue(traceContext.ConnectionContext, out var entry))
        {
            entry.ResponseMessage = responseMessage;
        }
    }
}
