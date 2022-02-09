namespace Localtunnel.Cli;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Cli.Configuration;
using Localtunnel.Endpoints;
using Localtunnel.Handlers;
using Localtunnel.Handlers.Kestrel;
using Localtunnel.Handlers.Passthrough;
using Localtunnel.Processors;
using Localtunnel.Properties;
using Localtunnel.Tracing;
using Localtunnel.Tunnels;
using Microsoft.Extensions.Logging;

internal sealed class CliBootstrapper
{
    // Static classes would not be allowed to use as a type param
    private CliBootstrapper() => throw new InvalidOperationException();

    public static async Task RunAsync(BaseConfiguration configuration, ITunnelEndpointFactory tunnelEndpointFactory, CancellationToken cancellationToken)
    {
        using var loggerFactory = LoggerFactory.Create(x => x
            .AddConsole()
            .SetMinimumLevel(configuration.Verbose ? LogLevel.Trace : LogLevel.Information));

        var clientLogger = loggerFactory.CreateLogger<LocaltunnelClient>();
        using var client = new LocaltunnelClient(new Uri(configuration.Server!), clientLogger);

        var connections = Math.Min(10, configuration.MaxConnections);
        clientLogger.LogDebug(Resources.CreatingTunnelWithNConnections, connections);

        var tunnelTraceListener = new HistoryTraceListener();

        ITunnelConnectionHandler tunnelConnectionHandler;
        if (configuration.Passthrough)
        {
            tunnelConnectionHandler = new PassthroughTunnelConnectionHandler(tunnelEndpointFactory);
        }
        else
        {
            var pipeline = new HttpRequestProcessingPipelineBuilder()
                .Append(new HttpHostHeaderRewritingRequestProcessor(configuration.Host!))
                .Build();

            tunnelConnectionHandler = new KestrelTunnelConnectionHandler(pipeline, tunnelEndpointFactory);
        }

        using var tunnel = await client
            .OpenAsync(tunnelConnectionHandler, configuration.Subdomain, tunnelTraceListener, cancellationToken)
            .ConfigureAwait(false);

        await tunnel.StartAsync(connections);

        if (configuration.Browser)
        {
            clientLogger.LogDebug(Resources.StartingBrowser, tunnel.Information.Url);
            StartBrowser(tunnel);
        }

        clientLogger.LogInformation(Resources.PressToExit);

        if (configuration.NoDashboard)
        {
            await WaitSilentAsync(cancellationToken);
        }
        else
        {
            await TunnelDashboard.Show(tunnel, tunnelTraceListener, cancellationToken);
        }

        clientLogger.LogInformation(Resources.ShuttingDown);
    }

    private static Process? StartBrowser(Tunnel tunnel)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = tunnel.Information.Url.ToString(),
            UseShellExecute = true,
        };

        return Process.Start(startInfo);
    }

    private static async Task WaitSilentAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(-1, cancellationToken);
        }
        catch (TaskCanceledException)
        {
        }
    }
}
