namespace Localtunnel.Http;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Localtunnel.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;

internal sealed class HttpTunnelConnectionContext : IHttpHeadersHandler, IHttpRequestLineHandler, IHttpTunnelConnectionContext
{
    private static readonly ReadOnlyMemory<byte> CrLf = new byte[] { (byte)'\r', (byte)'\n' };
    private static readonly ReadOnlyMemory<byte> Space = new byte[] { (byte)' ', };
    private static readonly ReadOnlyMemory<byte> ColonSpace = new byte[] { (byte)':', (byte)' ', };
    private static readonly ReadOnlyMemory<byte> Http11 = Encoding.ASCII.GetBytes("HTTP/1.1");

    private readonly ArrayBufferWriter<byte> _bufferWriter = new();
    private HttpParserState _state;
    private HttpRequest _httpRequest;
    private Dictionary<string, StringValues>? _headers;
    private HttpVersion _httpVersion;
    private string? _httpMethod;
    private string? _pathAndQuery;

    public HttpTunnelConnectionContext(ProxiedHttpTunnelConnection connection)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public ProxiedHttpTunnelConnection Connection { get; }

    public HttpRequest? HttpRequest => _state is HttpParserState.Body ? _httpRequest : null;

    public ReadOnlyMemory<byte> Buffer => _bufferWriter.WrittenMemory;

    public bool ProcessData(ReadOnlyMemory<byte> buffer)
    {
        if (_state is HttpParserState.Body)
        {
            return true;
        }

        var parser = new HttpParser<HttpTunnelConnectionContext>();
        var sequence = new ReadOnlySequence<byte>(buffer);
        ParseHttpRequest(parser, ref sequence, out _, out _);
        return _state is HttpParserState.Body;
    }

    private void ParseHttpRequest(HttpParser<HttpTunnelConnectionContext> parser, ref ReadOnlySequence<byte> inputBuffer, out SequencePosition consumed, out SequencePosition examined)
    {
        consumed = inputBuffer.Start;
        examined = inputBuffer.End;

        if (_state is HttpParserState.RequestLine && parser.ParseRequestLine(this, inputBuffer, out consumed, out examined))
        {
            _state = HttpParserState.Headers;
            _headers = new Dictionary<string, StringValues>();
            inputBuffer = inputBuffer.Slice(consumed);
        }

        if (_state is HttpParserState.Headers && parser.ParseHeaders(this, inputBuffer, out consumed, out examined, out _))
        {
            inputBuffer = inputBuffer.Slice(consumed);

            _httpRequest = new HttpRequest
            {
                Headers = _headers!.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase),
                HttpVersion = _httpVersion!,
                PathAndQuery = _pathAndQuery!,
                RequestMethod = _httpMethod!,
            };

            _state = HttpParserState.Body;
        }

        if (_state is HttpParserState.Body)
        {
            Connection.Options.RequestProcessor!.Process(this, ref _httpRequest);

            _bufferWriter.Write(Encoding.UTF8.GetBytes(_httpRequest.RequestMethod));
            _bufferWriter.Write(Space.Span);
            _bufferWriter.Write(Encoding.UTF8.GetBytes(_httpRequest.PathAndQuery));
            _bufferWriter.Write(Space.Span);
            _bufferWriter.Write(Http11.Span);
            _bufferWriter.Write(CrLf.Span);

            foreach (var (key, values) in _httpRequest.Headers)
            {

                _bufferWriter.Write(Encoding.UTF8.GetBytes(key));
                _bufferWriter.Write(ColonSpace.Span);
                _bufferWriter.Write(Encoding.UTF8.GetBytes(values));
                _bufferWriter.Write(CrLf.Span);
            }

            _bufferWriter.Write(CrLf.Span);

            // body
            foreach (var span in inputBuffer)
            {
                _bufferWriter.Write(span.Span);
            }
        }
    }

    /// <inheritdoc/>
    public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
    {
        _httpMethod = method switch
        {
            HttpMethod.Get => "GET",
            HttpMethod.Put => "PUT",
            HttpMethod.Delete => "DELETE",
            HttpMethod.Post => "POST",
            HttpMethod.Head => "HEAD",
            HttpMethod.Trace => "TRACE",
            HttpMethod.Patch => "PATCH",
            HttpMethod.Connect => "CONNECT",
            HttpMethod.Options => "OPTIONS",
            _ => Encoding.ASCII.GetString(customMethod),
        };

        _httpVersion = version;
        _pathAndQuery = Encoding.ASCII.GetString(target);
    }

    /// <inheritdoc/>
    public void OnHeader(Span<byte> name, Span<byte> value)
    {
        _headers![Encoding.UTF8.GetString(name)] = Encoding.UTF8.GetString(value);
    }
}

