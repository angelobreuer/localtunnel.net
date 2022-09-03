namespace Localtunnel.Cli.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Cli.Configuration;
using Localtunnel.Endpoints.Http;
using Localtunnel.Properties;

internal sealed class HttpsCliCommand : Command
{
    public HttpsCliCommand()
        : base("https", Resources.HttpsProxy)
    {
        // TODO
        Add(new Option<bool>(
            alias: "--allow-untrusted-certificates",
            description: Resources.AllowUntrustedCertificatesDescription));

        Handler = CommandHandler.Create<HttpsProxyConfiguration, CancellationToken>(RunAsync);
    }

    private Task RunAsync(HttpsProxyConfiguration configuration, CancellationToken cancellationToken)
    {
        return CliBootstrapper.RunAsync(
            configuration: configuration,
            tunnelEndpointFactory: new HttpsTunnelEndpointFactory(configuration.Host ?? "localhost", configuration.Port),
            cancellationToken: cancellationToken);
    }
}
