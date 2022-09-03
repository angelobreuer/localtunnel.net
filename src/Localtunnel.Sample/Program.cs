using Localtunnel;
using Localtunnel.Endpoints.Http;
using Localtunnel.Handlers.Kestrel;
using Localtunnel.Processors;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));

using var client = new LocaltunnelClient(loggerFactory);

var pipeline = new HttpRequestProcessingPipelineBuilder()
    .Append(new HttpHostHeaderRewritingRequestProcessor("test.angelobreuer.de"))
    .Build();

var endpointFactory = new HttpsTunnelEndpointFactory("test.angelobreuer.de", 443);
var tunnelConnectionHandler = new KestrelTunnelConnectionHandler(pipeline, endpointFactory);

var tunnel = await client
    .OpenAsync(tunnelConnectionHandler)
    .ConfigureAwait(false);

await tunnel.StartAsync().ConfigureAwait(false);

await Task.Delay(-1);