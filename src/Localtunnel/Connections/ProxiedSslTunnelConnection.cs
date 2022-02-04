namespace Localtunnel.Connections;

using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;

public class ProxiedSslTunnelConnection : ProxiedHttpTunnelConnection
{
    private readonly ProxiedSslTunnelOptions _options;

    public ProxiedSslTunnelConnection(TunnelConnectionHandle handle, ProxiedSslTunnelOptions options)
        : base(handle, options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    protected override Stream CreateProxyStream(Socket proxySocket)
    {
        var stream = base.CreateProxyStream(proxySocket);
        var sslStream = new SslStream(stream);

        var options = new SslClientAuthenticationOptions
        {
            TargetHost = _options.Host,
        };

        if (_options.AllowUntrustedCertificates)
        {
            options.RemoteCertificateValidationCallback = RemoteCertificateValidation.AllowAny;
        }

        sslStream.AuthenticateAsClient(options);

        return sslStream;
    }

    /// <inheritdoc/>
    protected override Uri GetBaseUri()
    {
        return new UriBuilder(Uri.UriSchemeHttps, Options.Host, Options.Port).Uri;
    }
}
