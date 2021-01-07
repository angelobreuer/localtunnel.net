namespace Localtunnel.Http
{
    using System.IO;
    using System.Net.Http;

    internal static class RequestWriter
    {
        private const string HTTP_EOL = "\r\n";

        public static void WriteRequest(TextWriter writer, HttpRequestMessage request, long contentLength)
        {
            // status line
            writer.Write(request.Method);
            writer.Write(' ');
            writer.Write(request.RequestUri!.PathAndQuery);
            writer.Write(" HTTP/1.1");
            writer.Write(HTTP_EOL);

            // content length
            writer.Write("Content-Length: ");
            writer.Write(contentLength);
            writer.Write(HTTP_EOL);

            // headers
            foreach (var (key, value) in request.Headers)
            {
                writer.Write(key);
                writer.Write(": ");
                writer.Write(string.Join(",", value));
                writer.Write(HTTP_EOL);
            }

            writer.Write(HTTP_EOL);
        }
    }
}
