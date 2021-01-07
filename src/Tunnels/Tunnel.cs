namespace Localtunnel.Tunnels
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Localtunnel.Connections;
    using Localtunnel.Properties;
    using Microsoft.Extensions.Logging;

    public class Tunnel
    {
        private readonly TunnelSocketContext[] _socketContexts;

        public Tunnel(TunnelInformation information, Func<TunnelConnectionHandle, TunnelConnection> connectionFactory, ArrayPool<byte>? arrayPool = null, ILogger? logger = null)
        {
            Information = information ?? throw new ArgumentNullException(nameof(information));
            ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            ArrayPool = arrayPool ?? ArrayPool<byte>.Shared;
            Logger = logger;

            _socketContexts = new TunnelSocketContext[Information.MaximumConnections];
        }

        public IEnumerable<TunnelConnection> Connections => _socketContexts.Select(x => x?.Connection).Where(x => x is not null)!;

        public TunnelInformation Information { get; }

        protected internal ArrayPool<byte> ArrayPool { get; }

        protected internal Func<TunnelConnectionHandle, TunnelConnection> ConnectionFactory { get; }

        protected internal ILogger? Logger { get; }

        public void Start(int connections = 10, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // perform DNS resolution once
            if (!IPAddress.TryParse(Information.Url.Host, out var ipAddress))
            {
                // TODO Async
                var ipHostEntry = Dns.GetHostEntry(Information.Url.DnsSafeHost);

                ipAddress = ipHostEntry.AddressList.FirstOrDefault()
                    ?? throw new Exception(string.Format(Resources.DnsResolutionFailed, Information.Url.DnsSafeHost));
            }

            var endPoint = new IPEndPoint(ipAddress, Information.Port);

            for (var index = 0; index < Math.Min(connections, Information.MaximumConnections); index++)
            {
                _socketContexts[index] = new TunnelSocketContext(this, endPoint, $"SocketContext-" + index);
                _socketContexts[index].BeginConnect();
            }
        }
    }
}
