namespace Localtunnel.CommandLine
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Localtunnel.Connections;
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
            using var client = new LocaltunnelClient(clientLogger);

            var connections = Math.Min(10, configuration.MaxConnections);
            clientLogger.LogDebug("Creating tunnel with {Connections} concurrent connection(s).", connections);

            var tunnel = await client.OpenAsync(connectionFactory, configuration.Subdomain);
            tunnel.Start(connections);

            if (configuration.Browser)
            {
                clientLogger.LogDebug("Starting browser at {Url}...", tunnel.Information.Url);
                StartBrowser(tunnel);
            }

            clientLogger.LogInformation("Press [Ctrl] + [C] to exit.");
            await TunnelDashboard.Show(tunnel, configuration, cancellationToken);

            clientLogger.LogInformation("Shutting down...");
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
    }
}
