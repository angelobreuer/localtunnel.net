namespace Localtunnel.CommandLine.Commands
{
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Threading;
    using System.Threading.Tasks;
    using Localtunnel.Connections;
    using Localtunnel.Properties;

    internal sealed class HttpCliCommand : Command
    {
        public HttpCliCommand()
            : base("http", Resources.HttpProxy)
        {
            Handler = CommandHandler.Create<HttpProxyConfiguration, CancellationToken>(RunAsync);
        }

        private Task RunAsync(HttpProxyConfiguration configuration, CancellationToken cancellationToken)
        {
            var options = new ProxiedHttpTunnelOptions
            {
                Host = configuration.Host,
                ReceiveBufferSize = configuration.ReceiveBufferSize,
                Port = configuration.Port,
            };

            if (configuration.Passthrough)
            {
                options.RequestProcessor = null;
            }

            return CliBootstrapper.RunAsync(configuration, x => new ProxiedHttpTunnelConnection(x, options), cancellationToken);
        }
    }
}
