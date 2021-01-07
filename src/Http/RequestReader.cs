namespace Localtunnel.Http
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    internal static class RequestReader
    {
        public static HttpRequestMessage? Parse(TextReader textReader, Uri baseUri)
        {
            var statusLine = textReader.ReadLine();

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
            ReadHttpHeaders(textReader, requestMessage.Headers);

            return requestMessage;
        }

        private static Version GetHttpVersion(string versionString) => versionString.ToUpperInvariant() switch
        {
            "HTTP/1.0" => HttpVersion.Version10,
            "HTTP/1.1" => HttpVersion.Version11,
            "HTTP/2.0" => HttpVersion.Version20,
            _ => HttpVersion.Unknown,
        };

        private static void ReadHttpHeaders(TextReader textReader, HttpRequestHeaders headers)
        {
            string? line;
            while (!string.IsNullOrWhiteSpace(line = textReader.ReadLine()))
            {
                var index = line.IndexOf(':');

                if (index is -1)
                {
                    // invalid header, .. ignore, just continue TODO?
                    continue;
                }

                var key = line[0..index].Trim();
                var value = line[(index + 1)..].Trim();

                headers.Add(key, value);
            }
        }
    }
}
