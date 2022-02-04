namespace Localtunnel.Tunnels;

using System;
using System.Text.Json.Serialization;

public sealed class TunnelInformation
{
    [JsonInclude]
    [JsonPropertyName("id")]
    public string Id { get; private set; } = null!;

    [JsonInclude]
    [JsonPropertyName("max_conn_count")]
    public int MaximumConnections { get; private set; }

    [JsonInclude]
    [JsonPropertyName("port")]
    public int Port { get; private set; }

    [JsonIgnore]
    public int ReceiveBufferSize { get; set; } = 64 * 1024;

    [JsonInclude]
    [JsonPropertyName("url")]
    public Uri Url { get; private set; } = null!;
}
