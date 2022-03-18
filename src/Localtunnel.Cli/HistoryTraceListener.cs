namespace Localtunnel.Tracing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Localtunnel.Cli;
using Localtunnel.Handlers;
using Localtunnel.Tunnels;

public sealed class HistoryTraceListener : TunnelTraceListener
{
    private readonly ConcurrentQueue<HttpTransactionEntry> _history;
    private readonly ConcurrentDictionary<ITunnelConnectionContext, HttpTransactionEntry> _associationMap;
    private readonly int _maxHistorySize;

    public HistoryTraceListener(int maxHistorySize = 10)
    {
        _history = new ConcurrentQueue<HttpTransactionEntry>();
        _maxHistorySize = maxHistorySize;
        _associationMap = new ConcurrentDictionary<ITunnelConnectionContext, HttpTransactionEntry>();
        _history = new ConcurrentQueue<HttpTransactionEntry>();
    }

    public IEnumerable<HttpTransactionEntry> Entries => _history.Take(_maxHistorySize); // need to limit because in high concurrency situations there may be more items returned than allowed

    protected internal override void OnConnectionStarted(TunnelConnectionTraceContext traceContext, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
    {
        base.OnConnectionStarted(traceContext, localEndPoint, remoteEndPoint);

        var traceEntry = new HttpTransactionEntry(traceContext.ConnectionContext, localEndPoint, remoteEndPoint, default);
        _history.Enqueue(traceEntry);
        _associationMap[traceContext.ConnectionContext] = traceEntry;

        while (_history.Count > _maxHistorySize && _history.TryDequeue(out _))
        {
        }
    }

    /// <inheritdoc/>
    protected internal override void OnConnectionCompleted(TunnelConnectionTraceContext traceContext)
    {
        base.OnConnectionCompleted(traceContext);

        _associationMap.TryRemove(traceContext.ConnectionContext, out _);
    }

    /// <inheritdoc/>
    protected internal override void OnHttpRequestStarted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage, ref Stream bodyReader)
    {
        base.OnHttpRequestStarted(traceContext, requestMessage, ref bodyReader);

        if (!_associationMap.TryGetValue(traceContext.ConnectionContext, out var entry))
        {
            return;
        }

        entry.SnapshotRecorder.RequestStream = bodyReader;
        bodyReader = entry.SnapshotRecorder;

        if (entry.RequestMessage is not null)
        {
            // new request
            entry = new HttpTransactionEntry(
                connectionContext: traceContext.ConnectionContext,
                localEndPoint: traceContext.ConnectionContext.LocalEndPoint,
                remoteEndPoint: traceContext.ConnectionContext.RemoteEndPoint,
                statisticsOffset: traceContext.ConnectionContext.Statistics);

            _history.Enqueue(entry);
            _associationMap[traceContext.ConnectionContext] = entry;
        }

        entry.RequestMessage = requestMessage;
    }

    /// <inheritdoc/>
    protected internal override void OnHttpRequestCompleted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage, Stream bodyReader)
    {
        base.OnHttpRequestCompleted(traceContext, requestMessage, bodyReader);

        var result = _associationMap.TryGetValue(traceContext.ConnectionContext, out var entry);

        Debug.Assert(result && entry is not null);
        Debug.Assert(ReferenceEquals(entry.SnapshotRecorder, bodyReader));

        entry.SnapshotRecorder.Snapshot(out var bodyIn, out _);
        entry.RequestBody = bodyIn;
    }

    /// <inheritdoc/>
    protected internal override void OnHttpResponseStarted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage, HttpResponseMessage responseMessage, ref Stream bodyWriter)
    {
        base.OnHttpResponseStarted(traceContext, requestMessage, responseMessage, ref bodyWriter);

        var result = _associationMap.TryGetValue(traceContext.ConnectionContext, out var entry);
        Debug.Assert(result && entry is not null);

        entry.ResponseMessage = responseMessage;

        entry.SnapshotRecorder.ResponseStream = bodyWriter;
        bodyWriter = entry.SnapshotRecorder;
    }

    /// <inheritdoc/>
    protected internal override void OnHttpResponseCompleted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage, HttpResponseMessage responseMessage, Stream bodyWriter)
    {
        base.OnHttpResponseCompleted(traceContext, requestMessage, responseMessage, bodyWriter);

        var result = _associationMap.TryRemove(traceContext.ConnectionContext, out var entry);
        Debug.Assert(result && entry is not null);

        entry.SnapshotRecorder.Snapshot(out _, out var bodyOut);
        entry.ResponseBody = bodyOut;
        entry.MarkCompleted();
    }
}
