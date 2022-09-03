namespace Localtunnel.Cli;

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Localtunnel.Connections;
using Localtunnel.Handlers;

public sealed class HttpTransactionEntry
{
    private readonly ConnectionStatistics _statisticsOffset;
    private readonly Stopwatch _stopwatch;

    public HttpTransactionEntry(ITunnelConnectionContext connectionContext, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, ConnectionStatistics statisticsOffset)
    {
        RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        ConnectionContext = connectionContext ?? throw new ArgumentNullException(nameof(connectionContext));
        LocalEndPoint = localEndPoint ?? throw new ArgumentNullException(nameof(localEndPoint));
        SnapshotRecorder = new StreamSnapshotRecorder();
        _statisticsOffset = statisticsOffset;
        StartedAt = DateTimeOffset.UtcNow;
        TransactionId = Guid.NewGuid();

        _stopwatch = new Stopwatch();
        _stopwatch.Restart();
    }

    public Guid TransactionId { get; }

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

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? EndedAt { get; private set; }

    internal StreamSnapshotRecorder SnapshotRecorder { get; }

    public StreamSnapshot<byte> RequestBody { get; set; }

    public StreamSnapshot<byte> ResponseBody { get; set; }

    public HttpRequestMessage? RequestMessage { get; set; }

    public HttpResponseMessage? ResponseMessage { get; set; }

    public ITunnelConnectionContext ConnectionContext { get; }

    public bool IsCompleted => ResponseMessage is not null || !ConnectionContext.IsOpen;

    internal void MarkCompleted()
    {
        _stopwatch.Stop();
        EndedAt = StartedAt + _stopwatch.Elapsed;
    }
}
