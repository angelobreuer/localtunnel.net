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

        public static Tuple<HttpRequestMessage, string, string, string>? Parse(ref ReadOnlySpan<byte> span, Uri baseUri)
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


            // ---------------------------------------------------- Shahid Change here to get content as well if needed


            // read headers
            var ct = ReadHttpHeaders(ref span, requestMessage.Headers);

            return new Tuple<HttpRequestMessage, String, String, String>(requestMessage, ct.Item1, ct.Item2, ct.Item3);
        }

        private static Version GetHttpVersion(string versionString) => versionString.ToUpperInvariant() switch
        {
            "HTTP/1.0" => HttpVersion.Version10,
            "HTTP/1.1" => HttpVersion.Version11,
            "HTTP/2.0" => HttpVersion.Version20,
            _ => HttpVersion.Unknown,
        };

        private static Tuple<string,string,string> ReadHttpHeaders(ref ReadOnlySpan<byte> span, HttpRequestHeaders headers)
        {
            string? line;
            string contenttype = "";
            string contentencoding = "";
            string contentlanguage = "";

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



                // -------------------------------------------------------------------------------------------------- Shahid Change: content headers need to be parsed as well. 
                if (key.ToLower() == "content-type") contenttype = value; // .Add(key, value);
                else if (key.ToLower() == "content-encoding") contentencoding += value + " ";
                else if (key.ToLower() == "content-language") contentlanguage += value + " ";
                else headers.TryAddWithoutValidation(key, value);

                

            }

            return new Tuple<string, string, string>(contenttype, contentencoding, contentlanguage);
        }
    }
}
