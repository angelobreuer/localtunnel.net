namespace Localtunnel;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Handlers;
using Localtunnel.Properties;
using Localtunnel.Tunnels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class LocaltunnelClient : IDisposable
{
    public static readonly Uri DefaultBaseAddress = new("http://localtunnel.me/"); // TODO https

    private readonly HttpClient _httpClient;
    private readonly ILogger<LocaltunnelClient> _logger;
    private readonly ConcurrentDictionary<Tunnel, bool> _tunnels;
    private readonly ILoggerFactory? _loggerFactory;

    public LocaltunnelClient(ILoggerFactory? loggerFactory = null)
        : this(DefaultBaseAddress, loggerFactory)
    {
    }

    public LocaltunnelClient(Uri baseAddress, ILoggerFactory? loggerFactory = null)
    {
        if (baseAddress is null)
        {
            throw new ArgumentNullException(nameof(baseAddress));
        }

        _tunnels = new ConcurrentDictionary<Tunnel, bool>();
        _httpClient = new() { BaseAddress = baseAddress };
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory?.CreateLogger<LocaltunnelClient>() ?? NullLogger<LocaltunnelClient>.Instance;
    }

    public IEnumerable<Tunnel> Tunnels => _tunnels.Keys;

    /// <inheritdoc/>
    public void Dispose() => _httpClient.Dispose();

    public async Task<Tunnel> OpenAsync(ITunnelConnectionHandler tunnelConnectionHandler, string? subdomain = null, TunnelTraceListener? tunnelTraceListener = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        tunnelTraceListener ??= NullTunnelTraceListener.Instance;

        var tunnel = await OpenAsync(subdomain, cancellationToken);
        return new Tunnel(this, tunnel, tunnelConnectionHandler, tunnelTraceListener, _loggerFactory);
    }

    public async Task<TunnelInformation> OpenAsync(string? subdomain = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(Resources.CreatingTunnel);

        var requestUri = subdomain is null ? "/?new" : string.Concat('/', subdomain);
        _logger.LogTrace(Resources.SendingHttpGet, requestUri);

        var response = (await _httpClient
            .GetFromJsonAsync<TunnelInformation>(requestUri, cancellationToken)
            .ConfigureAwait(false))!;

        _logger.LogInformation(Resources.TunnelCreated, response.Id, response.MaximumConnections, response.Port, response.Url);

        return response;
    }

    internal bool TryRegister(Tunnel tunnel)
    {
        return _tunnels.TryAdd(tunnel, false);
    }

    internal bool TryUnregister(Tunnel tunnel)
    {
        return _tunnels.TryRemove(tunnel, out _);
    }
}
