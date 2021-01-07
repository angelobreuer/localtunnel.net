namespace Localtunnel.Http
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    internal static class RequestReader
    {
        private static readonly byte[] _eol = new byte[] { (byte)'\r', (byte)'\n' };

        private static string? ReadLine(ref ReadOnlySpan<byte> span)
        {
            var start = span.IndexOf(_eol);

            if (start is -1)
            {
                return null;
            }

            var content = span[0..start];
            span = span[(start + 2)..];
            return Encoding.UTF8.GetString(content);
        }

        public static HttpRequestMessage? Parse(ref ReadOnlySpan<byte> span, Uri baseUri)
        {
            var statusLine = ReadLine(ref span);

            if (string.IsNullOrWhiteSpace(statusLine))
            {
                return null;
            }

            var parts = statusLine.Split(' ', StringSplitOptions.TrimEntries);

            if (parts.Length is not 3)
            {
                return null;
            }

            var requestMessage = new HttpRequestMessage
            {
                Method = new HttpMethod(parts[0].ToUpperInvariant()),
                RequestUri = new Uri(baseUri, parts[1]),
                Version = GetHttpVersion(parts[2]),
            };

            // read headers
            ReadHttpHeaders(ref span, requestMessage.Headers);

            return requestMessage;
        }

        private static Version GetHttpVersion(string versionString) => versionString.ToUpperInvariant() switch
        {
            "HTTP/1.0" => HttpVersion.Version10,
            "HTTP/1.1" => HttpVersion.Version11,
            "HTTP/2.0" => HttpVersion.Version20,
            _ => HttpVersion.Unknown,
        };

        private static void ReadHttpHeaders(ref ReadOnlySpan<byte> span, HttpRequestHeaders headers)
        {
            string? line;
            while (!string.IsNullOrWhiteSpace(line = ReadLine(ref span)))
            {
                var index = line.IndexOf(':');

                if (index is -1)
                {
                    // invalid header, .. ignore, just continue TODO?
                    continue;
                }

                var key = line[0..index].Trim();
                var value = line[(index + 1)..].Trim();

                headers.TryAddWithoutValidation(key, value);
            }
        }
    }
}
