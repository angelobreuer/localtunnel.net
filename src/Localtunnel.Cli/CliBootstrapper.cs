namespace Localtunnel.Cli
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Localtunnel.Cli.Configuration;
    using Localtunnel.Connections;
    using Localtunnel.Properties;
    using Localtunnel.Tunnels;
    using Microsoft.Extensions.Logging;

    internal sealed class CliBootstrapper
    {
        private CliBootstrapper() => throw new InvalidOperationException();

        public static async Task RunAsync(BaseConfiguration configuration, Func<TunnelConnectionHandle, TunnelConnection> connectionFactory, CancellationToken cancellationToken)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (connectionFactory is null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            using var loggerFactory = LoggerFactory.Create(x => x
                .AddConsole()
                .SetMinimumLevel(configuration.Verbose ? LogLevel.Trace : LogLevel.Information));

            var clientLogger = loggerFactory.CreateLogger<LocaltunnelClient>();
            using var client = new LocaltunnelClient(new Uri(configuration.Server), clientLogger);

            var connections = Math.Min(10, configuration.MaxConnections);
            clientLogger.LogDebug(Resources.CreatingTunnelWithNConnections, connections);

            using var tunnel = await client.OpenAsync(connectionFactory, configuration.Subdomain, cancellationToken);
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
                await TunnelDashboard.Show(tunnel, configuration, cancellationToken);
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
}
