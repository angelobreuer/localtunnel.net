namespace Localtunnel.Server;
internal sealed class TunnelRequestHandler
{
    private readonly ITunnelDnsProvider _tunnelDnsProvider;
    private readonly ITunnelService _tunnelService;

    public TunnelRequestHandler(ITunnelDnsProvider tunnelDnsProvider!!, ITunnelService tunnelService!!)
    {
        _tunnelDnsProvider = tunnelDnsProvider;
        _tunnelService = tunnelService;
    }

    private async ValueTask<bool> HandleNewTunnelAsync(HttpContext httpContext)
    {
        // Assertions: Query contains /?new

        if (!_tunnelDnsProvider.TryGetTunnel(httpContext.Request.Host, subdomainName: null, out var tunnelId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return false;
        }

        var tunnel = _tunnelService.GetTunnel(tunnelId);

        await httpContext.Response
            .WriteAsJsonAsync(tunnel.CreateInformationJsonObject())
            .ConfigureAwait(false);

        return true;
    }

    private async ValueTask<bool> HandleCreateOrGetTunnelAsync(HttpContext httpContext, string tunnelName)
    {
        // Assertions: First path segment is /{id}/
        if (!_tunnelDnsProvider.TryGetTunnel(httpContext.Request.Host, tunnelName, out var tunnelId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return false;
        }

        var tunnel = _tunnelService.GetTunnel(tunnelId);

        await httpContext.Response
            .WriteAsJsonAsync(tunnel.CreateInformationJsonObject())
            .ConfigureAwait(false);

        return true;
    }

    public async Task HandleRequestAsync(HttpContext httpContext, Func<Task> next)
    {
        if (!await HandleRequestInternalAsync(httpContext).ConfigureAwait(false))
        {
            await next().ConfigureAwait(false);
        }
    }

    private async Task<bool> HandleRequestInternalAsync(HttpContext httpContext)
    {
        if (!_tunnelDnsProvider.TryGetTunnel(httpContext.Request.Host, out var tunnelId))
        {
            // ?new
            if (httpContext.Request.Path.Equals("/") && httpContext.Request.Query.ContainsKey("new"))
            {
                return await HandleNewTunnelAsync(httpContext).ConfigureAwait(false);
            }

            // /{id}/
            var parts = httpContext.Request.Path.Value!.Split('/', StringSplitOptions.TrimEntries);

            if (parts.Length is 2 && string.IsNullOrEmpty(parts[0]))
            {
                return await HandleCreateOrGetTunnelAsync(httpContext, parts[1]).ConfigureAwait(false);
            }

            return false;
        }

        return await HandleEndpointAsync(httpContext, tunnelId).ConfigureAwait(false);
    }

    private async ValueTask<bool> HandleEndpointAsync(HttpContext httpContext, TunnelId tunnelId)
    {
        if (!_tunnelService.TryGetTunnel(tunnelId, out var tunnelInformation))
        {
            return false;
        }

        await tunnelInformation
            .HandleHttpConnection(httpContext.Request, httpContext.Response, httpContext.RequestAborted)
            .ConfigureAwait(false);

        return true;
    }
}