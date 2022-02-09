namespace Localtunnel.Handlers.Kestrel;

using Localtunnel.Tracing;

internal interface ITunnelTraceContextFeature
{
    TunnelConnectionTraceContext TraceContext { get; }
}