namespace Localtunnel.Http;

using System;
using Localtunnel.Connections;

public interface IHttpTunnelConnectionContext
{
    ProxiedHttpTunnelConnection Connection { get; }

    HttpRequest? HttpRequest { get; }

    bool ProcessData(ReadOnlyMemory<byte> buffer);
}