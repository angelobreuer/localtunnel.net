﻿namespace Localtunnel.Handlers.Kestrel;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Localtunnel.Connections;
using Localtunnel.Endpoints;
using Localtunnel.Processors;
using Localtunnel.Tracing;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

internal sealed class KestrelHttpServerContext : IHttpApplication<KestrelHttpServerConnectionContext>
{
    private readonly ITunnelEndpointFactory _tunnelEndpointFactory;
    private readonly IHttpRequestProcessingPipeline _httpRequestProcessingPipeline;
    private readonly HttpClient _httpClient;

    private static readonly HashSet<string> _headersToStrip = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Transfer-Encoding", "Connection", };

    public KestrelHttpServerContext(ITunnelAcceptorProvider tunnelAcceptorProvider, ITunnelEndpointFactory tunnelEndpointFactory, IHttpRequestProcessingPipeline httpRequestProcessingPipeline)
    {
        _tunnelEndpointFactory = tunnelEndpointFactory;
        _httpRequestProcessingPipeline = httpRequestProcessingPipeline;

        var loggerFactory = LoggerFactory.Create(x => x.AddConsole()); // TODO
        var serverOptions = new KestrelServerOptions();
        serverOptions.ListenAnyIP(0); // this is a dummy value to bypass kestrel's address detection feature

        var kestrelServerOptionsWrapper = new OptionsWrapper<KestrelServerOptions>(serverOptions);
        var transportFactory = new TunnelTransportFactory(tunnelAcceptorProvider);
        Server = new KestrelServer(kestrelServerOptionsWrapper, transportFactory, loggerFactory);

        var httpClientHandler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,

            ConnectCallback = async (context, cancellationToken) =>
            {
                var endpoint = _tunnelEndpointFactory.Create();

                var stream = await endpoint
                    .CreateStreamAsync(cancellationToken)
                    .ConfigureAwait(false);

                return stream;
            },
        };

        _httpClient = new HttpClient(httpClientHandler, disposeHandler: true);
    }

    public KestrelServer Server { get; }

    /// <inheritdoc/>
    KestrelHttpServerConnectionContext IHttpApplication<KestrelHttpServerConnectionContext>.CreateContext(IFeatureCollection contextFeatures)
    {
        ArgumentNullException.ThrowIfNull(contextFeatures);

        var traceContext = contextFeatures.Get<TunnelTraceContextFeature>()!.TraceContext;
        return new KestrelHttpServerConnectionContext(contextFeatures, traceContext);
    }

    /// <inheritdoc/>
    void IHttpApplication<KestrelHttpServerConnectionContext>.DisposeContext(KestrelHttpServerConnectionContext context, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(context);
    }

    /// <inheritdoc/>
    async Task IHttpApplication<KestrelHttpServerConnectionContext>.ProcessRequestAsync(KestrelHttpServerConnectionContext context)
    {
        var httpContext = new DefaultHttpContext(context.Features);

        try
        {
            await _httpRequestProcessingPipeline
                .HandleRequestAsync(httpContext, httpContext.RequestAborted)
                .ConfigureAwait(false);

            await HandleRequestAsync(httpContext, context.TraceContext, httpContext.RequestAborted);
        }
        catch (Exception)
        {
            // TODO log
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }
    }

    private async ValueTask HandleRequestAsync(HttpContext httpContext, TunnelConnectionTraceContext traceContext, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var isUpgradeConnection = httpContext.Request.Headers.Upgrade.Count > 0;
        var tunnelConnectionContext = (KestrelTunnelConnectionContext)traceContext.ConnectionContext;

        using var statisticsStream = new StatisticsStream(
            requestStream: httpContext.Request.Body,
            responseStream: httpContext.Response.Body,
            connectionContext: tunnelConnectionContext);

        using var httpContent = isUpgradeConnection
            ? null // Do not forward body for upgrade connections
            : new StreamContent(statisticsStream);

        var requestMessage = new HttpRequestMessage
        {
            Method = GetHttpMethodCachedOrCreate(httpContext.Request.Method),
            Content = httpContent,
            RequestUri = new Uri(httpContext.Request.GetEncodedUrl()),
        };

        foreach (var (header, value) in httpContext.Request.Headers)
        {
            var values = value.ToString();

            if (!requestMessage.Headers.TryAddWithoutValidation(header, values) && !isUpgradeConnection)
            {
                httpContent!.Headers.TryAddWithoutValidation(header, values);
            }
        }

        tunnelConnectionContext.RequestMessage = requestMessage;

        var originalBodyReader = statisticsStream;
        var bodyReader = (Stream)originalBodyReader;
        traceContext.OnHttpRequestStarted(ref bodyReader);

        if (!ReferenceEquals(bodyReader, originalBodyReader) && !isUpgradeConnection)
        {
            // body writer changed, replace stream
            requestMessage.Content = new StreamContent(bodyReader);
        }

        var completionOption = isUpgradeConnection
            ? HttpCompletionOption.ResponseHeadersRead
            : HttpCompletionOption.ResponseContentRead;

        // send request
        using var responseMessage = await _httpClient
            .SendAsync(requestMessage, completionOption, cancellationToken)
            .ConfigureAwait(false);

        tunnelConnectionContext.ResponseMessage = responseMessage;

        traceContext.OnHttpRequestCompleted(bodyReader);

        var bodyWriter = (Stream)originalBodyReader;

        traceContext.OnHttpResponseStarted(ref bodyWriter);

        // copy headers to response
        foreach (var (header, value) in responseMessage.Headers)
        {
            if (_headersToStrip.Contains(header))
            {
                continue;
            }

            httpContext.Response.Headers.TryAdd(header, new StringValues((string[])value!));
        }

        foreach (var (header, value) in responseMessage.Content.Headers)
        {
            if (_headersToStrip.Contains(header))
            {
                continue;
            }

            httpContext.Response.Headers.TryAdd(header, new StringValues((string[])value!));
        }

        // copy status code
        httpContext.Response.StatusCode = (int)responseMessage.StatusCode;

        // copy body
        if (!isUpgradeConnection)
        {
            await responseMessage.Content
                .CopyToAsync(bodyWriter, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var upgradeConnectionFeature = httpContext.Features.Get<IHttpUpgradeFeature>()!;
            using var clientStream = await upgradeConnectionFeature.UpgradeAsync().ConfigureAwait(false);
            using var serverStream = responseMessage.Content.ReadAsStream(cancellationToken);

            var task1 = clientStream.CopyToAsync(serverStream, cancellationToken);
            var task2 = serverStream.CopyToAsync(clientStream, cancellationToken);

            await Task.WhenAll(task1, task2).ConfigureAwait(false);
        }

        await httpContext.Response
            .CompleteAsync()
            .ConfigureAwait(false);

        traceContext.OnHttpResponseCompleted(bodyWriter);
    }

    private static HttpMethod GetHttpMethodCachedOrCreate(string httpMethod) => httpMethod.ToUpperInvariant() switch
    {
        "DELETE" => HttpMethod.Delete,
        "GET" => HttpMethod.Get,
        "HEAD" => HttpMethod.Head,
        "OPTIONS" => HttpMethod.Options,
        "PATCH" => HttpMethod.Patch,
        "POST" => HttpMethod.Post,
        "PUT" => HttpMethod.Put,
        "TRACE" => HttpMethod.Trace,
        _ => new HttpMethod(httpMethod),
    };

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Server.StartAsync(this, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Server.StopAsync(cancellationToken);
    }

    private sealed class StatisticsStream : Stream
    {
        private readonly Stream _requestStream;
        private readonly Stream _responseStream;
        private readonly KestrelTunnelConnectionContext _connectionContext;

        public StatisticsStream(Stream requestStream, Stream responseStream, KestrelTunnelConnectionContext connectionContext)
        {
            _requestStream = requestStream;
            _responseStream = responseStream;
            _connectionContext = connectionContext;
        }

        public ConnectionStatistics Statistics
        {
            get => _connectionContext.Statistics;
            private set => _connectionContext.Statistics = value;
        }

        /// <inheritdoc/>
        public override bool CanRead => _requestStream.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => _requestStream.CanWrite;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            _responseStream.Flush();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = _requestStream.Read(buffer, offset, count);
            Statistics = Statistics with { BytesIn = Statistics.BytesIn + count, };
            return bytesRead;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _responseStream.Write(buffer, offset, count);
            Statistics = Statistics with { BytesOut = Statistics.BytesOut + count, };
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _responseStream
                .WriteAsync(buffer.AsMemory(offset, count), cancellationToken)
                .ConfigureAwait(false);

            Statistics = Statistics with { BytesOut = Statistics.BytesOut + count, };
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _responseStream
                .WriteAsync(buffer, cancellationToken)
                .ConfigureAwait(false);

            Statistics = Statistics with { BytesOut = Statistics.BytesOut + buffer.Length, };
        }

        public override int Read(Span<byte> buffer)
        {
            var bytesRead = _requestStream.Read(buffer);
            Statistics = Statistics with { BytesIn = Statistics.BytesIn + bytesRead, };
            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await _requestStream
                .ReadAsync(buffer.AsMemory(offset, count), cancellationToken)
                .ConfigureAwait(false);

            Statistics = Statistics with { BytesIn = Statistics.BytesIn + bytesRead, };
            return bytesRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var bytesRead = await _requestStream
                .ReadAsync(buffer, cancellationToken)
                .ConfigureAwait(false);

            Statistics = Statistics with { BytesIn = Statistics.BytesIn + bytesRead, };
            return bytesRead;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _responseStream.Write(buffer);

            Statistics = Statistics with { BytesOut = Statistics.BytesOut + buffer.Length, };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

            _requestStream.Dispose();
            _responseStream.Dispose();
        }
    }
}
