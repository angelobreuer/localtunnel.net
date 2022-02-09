namespace Localtunnel.Tracing;

using System;
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

    public void OnHttpRequestStarted()
    {
        TraceListener.OnHttpRequestStarted(
            traceContext: this,
            requestMessage: ConnectionContext.RequestMessage!);
    }

    public void OnHttpRequestCompleted()
    {
        TraceListener.OnHttpRequestCompleted(
            traceContext: this,
            requestMessage: ConnectionContext.RequestMessage!);
    }

    public void OnHttpResponseStarted()
    {
        TraceListener.OnHttpResponseStarted(
            traceContext: this,
            requestMessage: ConnectionContext.RequestMessage!,
            responseMessage: ConnectionContext.ResponseMessage!);
    }

    public void OnHttpResponseCompleted()
    {
        TraceListener.OnHttpResponseStarted(
            traceContext: this,
            requestMessage: ConnectionContext.RequestMessage!,
            responseMessage: ConnectionContext.ResponseMessage!);
    }
}
