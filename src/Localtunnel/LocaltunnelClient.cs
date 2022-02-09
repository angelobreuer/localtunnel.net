namespace Localtunnel;

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Handlers;
using Localtunnel.Properties;
using Localtunnel.Tunnels;
using Microsoft.Extensions.Logging;

public sealed class LocaltunnelClient : IDisposable
{
    public static readonly Uri DefaultBaseAddress = new("http://localtunnel.me/"); // TODO https

    private readonly HttpClient _httpClient;
    private readonly ILogger<LocaltunnelClient>? _logger;

    public LocaltunnelClient(ILogger<LocaltunnelClient>? logger = null)
        : this(DefaultBaseAddress, logger)
    {
    }

    public LocaltunnelClient(Uri baseAddress, ILogger<LocaltunnelClient>? logger = null)
    {
        if (baseAddress is null)
        {
            throw new ArgumentNullException(nameof(baseAddress));
        }

        _httpClient = new() { BaseAddress = baseAddress };
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Dispose() => _httpClient.Dispose();

    public async Task<Tunnel> OpenAsync(ITunnelConnectionHandler tunnelConnectionHandler, string? subdomain = null, TunnelTraceListener? tunnelTraceListener = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        tunnelTraceListener ??= NullTunnelTraceListener.Instance;

        var tunnel = await OpenAsync(subdomain, cancellationToken);
        return new Tunnel(tunnel, tunnelConnectionHandler, tunnelTraceListener, _logger);
    }

    public async Task<TunnelInformation> OpenAsync(string? subdomain = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger?.LogInformation(Resources.CreatingTunnel);

        var requestUri = subdomain is null ? "/?new" : string.Concat('/', subdomain);
        _logger?.LogTrace(Resources.SendingHttpGet, requestUri);

        var response = (await _httpClient
            .GetFromJsonAsync<TunnelInformation>(requestUri, cancellationToken)
            .ConfigureAwait(false))!;

        _logger?.LogInformation(Resources.TunnelCreated, response.Id, response.MaximumConnections, response.Port, response.Url);

        return response;
    }
}
