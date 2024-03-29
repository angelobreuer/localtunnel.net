﻿namespace Localtunnel.Tunnels;

using System.IO;
using System.Net;
using System.Net.Http;
using Localtunnel.Handlers;
using Localtunnel.Tracing;

public abstract class TunnelTraceListener
{
    public TunnelConnectionTraceContext CreateContext(ITunnelConnectionContext tunnelConnectionContext)
    {
        return new TunnelConnectionTraceContext(this, tunnelConnectionContext);
    }

    protected internal virtual void OnConnectionStarted(TunnelConnectionTraceContext traceContext, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
    {
    }

    protected internal virtual void OnConnectionCompleted(TunnelConnectionTraceContext traceContext)
    {
    }

    protected internal virtual void OnHttpRequestStarted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage, ref Stream bodyReader)
    {
    }

    protected internal virtual void OnHttpRequestCompleted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage, Stream bodyReader)
    {
    }

    protected internal virtual void OnHttpResponseStarted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage, HttpResponseMessage responseMessage, ref Stream bodyWriter)
    {
    }

    protected internal virtual void OnHttpResponseCompleted(TunnelConnectionTraceContext traceContext, HttpRequestMessage requestMessage, HttpResponseMessage responseMessage, Stream bodyWriter)
    {
    }
}
