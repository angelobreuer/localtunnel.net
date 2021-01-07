namespace Localtunnel.CommandLine.Commands
{
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Threading;
    using System.Threading.Tasks;
    using Localtunnel.Connections;
    using Localtunnel.Properties;

    internal sealed class HttpsCliCommand : Command
    {
        public HttpsCliCommand()
            : base("https", Resources.HttpsProxy)
        {
            Add(new Option<bool>(
                alias: "--allow-untrusted-certificates",
                description: Resources.AllowUntrustedCertificatesDescription));

            Handler = CommandHandler.Create<HttpsProxyConfiguration, CancellationToken>(RunAsync);
        }

        private Task RunAsync(HttpsProxyConfiguration configuration, CancellationToken cancellationToken)
        {
            var options = new ProxiedSslTunnelOptions
            {
                Host = configuration.Host,
                ReceiveBufferSize = configuration.ReceiveBufferSize,
                Port = configuration.Port,
                AllowUntrustedCertificates = configuration.AllowUntrustedCertificates,
            };

            if (configuration.Passthrough)
            {
                options.RequestProcessor = null;
            }

            return CliBootstrapper.RunAsync(configuration, x => new ProxiedSslTunnelConnection(x, options), cancellationToken);
        }
    }
}
