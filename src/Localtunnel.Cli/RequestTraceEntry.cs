namespace Localtunnel.Cli;

using System;
using System.Net;
using System.Net.Http;
using Localtunnel.Connections;
using Localtunnel.Handlers;

public sealed class RequestTraceEntry
{
    private readonly ConnectionStatistics _statisticsOffset;

    public RequestTraceEntry(ITunnelConnectionContext connectionContext, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, ConnectionStatistics statisticsOffset)
    {
        RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        ConnectionContext = connectionContext ?? throw new ArgumentNullException(nameof(connectionContext));
        LocalEndPoint = localEndPoint ?? throw new ArgumentNullException(nameof(localEndPoint));
        _statisticsOffset = statisticsOffset;
    }

    public IPEndPoint RemoteEndPoint { get; }

    public IPEndPoint LocalEndPoint { get; }

    public ConnectionStatistics Statistics
    {
        get
        {
            var originalStatistics = ConnectionContext.Statistics;
            var bytesIn = originalStatistics.BytesIn - _statisticsOffset.BytesIn;
            var bytesOut = originalStatistics.BytesOut - _statisticsOffset.BytesOut;
            return new ConnectionStatistics(bytesIn, bytesOut);
        }
    }


    public HttpRequestMessage? RequestMessage { get; set; }

    public HttpResponseMessage? ResponseMessage { get; set; }

    public ITunnelConnectionContext ConnectionContext { get; }

    public bool IsCompleted => ResponseMessage is not null || !ConnectionContext.IsOpen;
}
