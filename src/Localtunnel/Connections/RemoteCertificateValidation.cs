namespace Localtunnel.Connections
{
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    internal static class RemoteCertificateValidation
    {
        public static readonly RemoteCertificateValidationCallback AllowAny = AllowAnyImpl;

        private static bool AllowAnyImpl(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
