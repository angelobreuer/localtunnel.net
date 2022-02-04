namespace Localtunnel.Cli.Configuration;

internal sealed class HttpsProxyConfiguration : BaseConfiguration
{
    public bool AllowUntrustedCertificates { get; set; }
}
