namespace Localtunnel.Cli.Configuration;

internal abstract class BaseConfiguration
{
    public bool Browser { get; set; }

    public string? Host { get; set; }

    public int MaxConnections { get; set; }

    public bool NoDashboard { get; set; }

    public bool Passthrough { get; set; }

    public int Port { get; set; }

    public int ReceiveBufferSize { get; set; }

    public string? Server { get; set; }

    public string? Subdomain { get; set; }

    public bool Verbose { get; set; }
}
