namespace Localtunnel.Http;

internal enum HttpParserState : byte
{
    RequestLine,
    Headers,
    Body,
}
