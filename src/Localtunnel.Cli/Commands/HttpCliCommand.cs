namespace Localtunnel.Cli.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Cli.Configuration;
using Localtunnel.Endpoints.Http;
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
        return CliBootstrapper.RunAsync(
            configuration: configuration,
            tunnelEndpointFactory: new HttpTunnelEndpointFactory(configuration.Host ?? "localhost", configuration.Port),
            cancellationToken: cancellationToken);
    }
}
