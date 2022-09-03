namespace Localtunnel.Handlers.Kestrel;

using Localtunnel.Tracing;

internal sealed class TunnelTraceContextFeature : ITunnelTraceContextFeature
{
    public TunnelTraceContextFeature(TunnelConnectionTraceContext traceContext)
    {
        TraceContext = traceContext;
    }

    public TunnelConnectionTraceContext TraceContext { get; }
}
