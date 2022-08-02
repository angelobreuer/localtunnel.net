namespace Localtunnel.Handlers.Kestrel;

using System;
using Localtunnel.Tracing;
using Microsoft.AspNetCore.Http.Features;

public class KestrelHttpServerConnectionContext
{
    public KestrelHttpServerConnectionContext(IFeatureCollection features, TunnelConnectionTraceContext traceContext)
    {
        ArgumentNullException.ThrowIfNull(features);

        Features = features;
        TraceContext = traceContext;
    }

    public TunnelConnectionTraceContext TraceContext { get; }

    public IFeatureCollection Features { get; }
}
