namespace Localtunnel.Http
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    internal static class RequestReader
    {
        private static readonly byte[] _eol = new byte[] { (byte)'\r', (byte)'\n' };

        private static List<string> ListKnownHeaders = new List<string> {
            "Allow", "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Location", "Content-MD5", "Content-Range", "Content-Type", "Expires", "Last-Modified"
        };

        private static HashSet<string> KnownHeadersSet = new HashSet<string>(ListKnownHeaders, StringComparer.OrdinalIgnoreCase);

        // Note content length is omitted as its recalculated anyways. Above are all headers as
        // per: https://docs.microsoft.com/en-us/dotnet/api/system.net.http.headers.httpcontentheaders?view=net-5.0

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

        public static Tuple<HttpRequestMessage, NameValueCollection>? Parse(ref ReadOnlySpan<byte> span, Uri baseUri)
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

            // ---------------------------------------------------- Shahid Change here to get
            // content as well if needed

            //var contentHeaders = new Dictionary<string, string>();
            var contentHeaders = new NameValueCollection();

            // read headers
            ReadHttpHeaders(ref span, requestMessage.Headers, contentHeaders);

            return new Tuple<HttpRequestMessage, NameValueCollection>(requestMessage, contentHeaders);
        }

        private static Version GetHttpVersion(string versionString) => versionString.ToUpperInvariant() switch
        {
            "HTTP/1.0" => HttpVersion.Version10,
            "HTTP/1.1" => HttpVersion.Version11,
            "HTTP/2.0" => HttpVersion.Version20,
            _ => HttpVersion.Unknown,
        };

        private static void ReadHttpHeaders(ref ReadOnlySpan<byte> span, HttpRequestHeaders headers, NameValueCollection contentHeaders)
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

                if (KnownHeadersSet.Contains(key))
                {
                    contentHeaders.Add(key, value);
                }
                else
                {
                    headers.TryAddWithoutValidation(key, value);
                }
            }
        }
    }
}
