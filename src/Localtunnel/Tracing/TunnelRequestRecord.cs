namespace Localtunnel.Tunnels;

using System.Net.Http;

public sealed record TunnelRequestRecord(HttpRequestMessage? RequestMessage, HttpResponseMessage ResponseMessage);