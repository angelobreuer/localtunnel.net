namespace Localtunnel.Tracing;

using System;
using System.IO;
using Localtunnel.Handlers;
using Localtunnel.Tunnels;

public readonly struct TunnelConnectionTraceContext
{
    public TunnelConnectionTraceContext(TunnelTraceListener traceListener, ITunnelConnectionContext connectionContext)
    {
        TraceListener = traceListener ?? throw new ArgumentNullException(nameof(traceListener));
        ConnectionContext = connectionContext ?? throw new ArgumentNullException(nameof(connectionContext));
    }

    public TunnelTraceListener TraceListener { get; }

    public ITunnelConnectionContext ConnectionContext { get; }

    public void OnConnectionStarted()
    {
        TraceListener.OnConnectionStarted(
            traceContext: this,
            localEndPoint: ConnectionContext.LocalEndPoint!,
            remoteEndPoint: ConnectionContext.RemoteEndPoint!);
    }

    public void OnConnectionCompleted()
    {
        TraceListener.OnConnectionCompleted(traceContext: this);
    }

    public void OnHttpRequestStarted(ref Stream bodyReader)
    {
        TraceListener.OnHttpRequestStarted(
            traceContext: this,
            requestMessage: ConnectionContext.RequestMessage!,
            bodyReader: ref bodyReader);
    }

    public void OnHttpRequestCompleted(Stream bodyReader)
    {
        TraceListener.OnHttpRequestCompleted(
            traceContext: this,
            requestMessage: ConnectionContext.RequestMessage!,
            bodyReader: bodyReader);
    }

    public void OnHttpResponseStarted(ref Stream bodyWriter)
    {
        TraceListener.OnHttpResponseStarted(
            traceContext: this,
            requestMessage: ConnectionContext.RequestMessage!,
            responseMessage: ConnectionContext.ResponseMessage!,
            bodyWriter: ref bodyWriter);
    }

    public void OnHttpResponseCompleted(Stream bodyWriter)
    {
        TraceListener.OnHttpResponseCompleted(
            traceContext: this,
            requestMessage: ConnectionContext.RequestMessage!,
            responseMessage: ConnectionContext.ResponseMessage!,
            bodyWriter: bodyWriter);
    }
}
