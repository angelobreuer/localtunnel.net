namespace Localtunnel.Endpoints;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface ITunnelEndpoint
{
    ValueTask<Stream> CreateStreamAsync(CancellationToken cancellationToken = default);
}