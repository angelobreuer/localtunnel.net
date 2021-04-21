namespace Localtunnel.Http
{
    using System.Collections.Specialized;
    using System.IO;
    using System.Net.Http;

    internal static class RequestWriter
    {
        private const string HTTP_EOL = "\r\n";

        public static void WriteRequest(TextWriter writer, HttpRequestMessage request, long contentLength, NameValueCollection contentHeaders)
        {

            // status line
            writer.Write(request.Method);
            writer.Write(' ');
            writer.Write(request.RequestUri!.PathAndQuery);
            writer.Write(" HTTP/1.1");
            writer.Write(HTTP_EOL);


            WriteHeader(writer, "Content-Length", contentLength.ToString());

            // ---------------------------------------------------------- Shahid Changes
            // ------------------------------------------------------------ Content headers were missing, and if they are not present and parsed, they will not be forwarded ahead leading to errors
            
            foreach(string key in contentHeaders)
            {
                WriteHeader(writer, key, contentHeaders[key]);
            }


            // headers
            foreach (var (key, value) in request.Headers)
            {
                WriteHeader(writer, key, string.Join(",", value));
            }



            writer.Write(HTTP_EOL);
        }

        public static void WriteHeader(TextWriter writer, string key, string? encodedValues)
        {
            if (encodedValues is not null && encodedValues != "")
            {
                writer.Write(key);
                writer.Write(": ");
                writer.Write(encodedValues);
                writer.Write(HTTP_EOL);
            }
        }
    }
}
