namespace Localtunnel.Server;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Localtunnel.Server.IO;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

internal sealed class TunnelInformation : IDisposable
{
    private readonly Channel<Socket> _connectionQueue;
    private readonly HttpClient _httpClient;
    private readonly TunnelService _tunnelService;
    private readonly ConcurrentDictionary<Socket, Stream> _openSockets;
    private bool _disposed;

    public TunnelInformation(TunnelId id, Socket serverSocket, TunnelService tunnelService)
    {
        ArgumentNullException.ThrowIfNull(serverSocket);
        ArgumentNullException.ThrowIfNull(tunnelService);

        Id = id;
        ServerSocket = serverSocket;
        _tunnelService = tunnelService;
        _openSockets = new ConcurrentDictionary<Socket, Stream>();

        BeginAccept(socketAsyncEventArgs: null);

        var socketsHttpHandler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectCallback = CreateStreamAsync,
            MaxConnectionsPerServer = 4,
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
        };

        _httpClient = new HttpClient(socketsHttpHandler);

        var channelOptions = new BoundedChannelOptions(MaximumConnections)
        {
            AllowSynchronousContinuations = true,
            SingleWriter = true,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.DropWrite,
        };

        _connectionQueue = Channel.CreateBounded<Socket>(channelOptions);
    }

    public Socket ServerSocket { get; }

    public TunnelId Id { get; }

    public int MaximumConnections { get; } = 10;

    public async ValueTask HandleHttpConnectionAsync(HttpRequest httpRequest, HttpResponse httpResponse, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var isUpgradeRequest = httpRequest.Headers.Upgrade.Any();

        using var httpContent = httpRequest.Body is not null && !isUpgradeRequest
            ? new StreamContent(httpRequest.Body)
            : null;

        var targetRequestUri = new UriBuilder(
            scheme: Uri.UriSchemeHttp,
            host: httpRequest.Host.Value,
            port: httpRequest.Host.Port.GetValueOrDefault(80),
            pathValue: httpRequest.Path.Value);

        targetRequestUri.Query = httpRequest.QueryString.Value;

        var httpRequestMessage = new HttpRequestMessage
        {
            Content = httpContent,
            Method = AttemptMapToCachedMethod(httpRequest.Method),
            RequestUri = targetRequestUri.Uri,
        };

        // headers
        foreach (var (key, values) in httpRequest.Headers)
        {
            var stringValues = values.ToString();

            if (!httpRequestMessage.Headers.TryAddWithoutValidation(key, stringValues) && httpContent is not null)
            {
                httpContent.Headers.TryAddWithoutValidation(key, stringValues);
            }
        }

        // add custom headers
        var remoteIpAddress = httpRequest.HttpContext.Connection.RemoteIpAddress;

        if (remoteIpAddress is not null)
        {
            // normalize IP address if IPv6
            var realIp = remoteIpAddress.AddressFamily is AddressFamily.InterNetworkV6
                ? $"[{remoteIpAddress}]"
                : remoteIpAddress.ToString();

            // add port if non standard
            var standardPort = httpRequest.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ? 80 : 443;

            if (httpRequest.HttpContext.Connection.RemotePort != standardPort)
            {
                realIp += $":{httpRequest.HttpContext.Connection.RemotePort}";
            }

            httpRequestMessage.Headers.TryAddWithoutValidation("X-Forwarded-For", realIp);
            httpRequestMessage.Headers.TryAddWithoutValidation("X-Real-IP", realIp);
        }

        httpRequestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Host", httpRequest.Host.Value);
        httpRequestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Proto", httpRequest.Scheme);

        // send request
        var responseMessage = await _httpClient
            .SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        httpResponse.StatusCode = (int)responseMessage.StatusCode;

        // copy to response
        foreach (var (key, value) in responseMessage.Headers)
        {
            httpResponse.Headers.TryAdd(key, new StringValues((string[])value!));
        }

        if (responseMessage.Content is not null)
        {
            foreach (var (key, value) in responseMessage.Content.Headers)
            {
                httpResponse.Headers.TryAdd(key, new StringValues((string[])value!));
            }
        }

        if (isUpgradeRequest)
        {
            await HandleUpgradeConnectionAsync(responseMessage, httpRequest, httpResponse, cancellationToken);
        }
        else if (responseMessage.Content is not null)
        {
            await HandleConnectionAsync(responseMessage, httpResponse, cancellationToken);
        }

        await httpResponse
            .CompleteAsync()
            .ConfigureAwait(false);
    }

    private async Task HandleUpgradeConnectionAsync(HttpResponseMessage responseMessage, HttpRequest httpRequest, HttpResponse httpResponse, CancellationToken cancellationToken = default)
    {
        var upgradeFeature = httpRequest.HttpContext.Features.Get<IHttpUpgradeFeature>()!;

        using var clientStream = responseMessage.Content.ReadAsStream(cancellationToken);
        using var serverStream = await upgradeFeature.UpgradeAsync().ConfigureAwait(false);

        var task1 = clientStream.CopyToAsync(serverStream, cancellationToken);
        var task2 = serverStream.CopyToAsync(clientStream, cancellationToken);

        await Task.WhenAll(task1, task2);
    }

    private async Task HandleConnectionAsync(HttpResponseMessage responseMessage, HttpResponse httpResponse, CancellationToken cancellationToken = default)
    {
        await responseMessage.Content
            .CopyToAsync(httpResponse.Body, cancellationToken)
            .ConfigureAwait(false);
    }

    private void NotifySocketClosed(Socket socket)
    {
        if (!_openSockets.Remove(socket, out var stream))
        {
            stream!.Close();
        }
    }

    private async ValueTask<Stream> CreateStreamAsync(SocketsHttpConnectionContext socketsHttpConnectionContext, CancellationToken cancellationToken = default)
    {
        static bool PollDisconnected(Socket socket)
        {
            return socket.Poll(1000, SelectMode.SelectRead) && socket.Available is 0;
        }

        void NotifySocketCleanedUpInternal(object? state)
        {
            var instance = (Socket)state!;
            NotifySocketClosed(instance);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var socket = await _connectionQueue.Reader
            .ReadAsync(cancellationToken)
            .ConfigureAwait(false);

        while (PollDisconnected(socket))
        {
            socket.Dispose();

            socket = await _connectionQueue.Reader
                .ReadAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var networkStream = new NetworkStream(socket, true);

        var delegatingStream = new DelegatingCloseStream(
            stream: networkStream,
            postCleanupCallback: NotifySocketCleanedUpInternal,
            state: socket);

        return _openSockets[socket] = delegatingStream;
    }

    internal void OnAccepted(AcceptSocketAsyncEventArgs socketAsyncEventArgs)
    {
        if (socketAsyncEventArgs.SocketError is not SocketError.Success)
        {
            SocketAsyncEventArgsPool.AcceptPool.Return(socketAsyncEventArgs);
            return;
        }

        Debug.Assert(ReferenceEquals(socketAsyncEventArgs.TunnelInformation, this));
        Debug.Assert(socketAsyncEventArgs.LastOperation is SocketAsyncOperation.Accept);

        var acceptSocket = socketAsyncEventArgs.AcceptSocket!;
        BeginAccept(socketAsyncEventArgs);

        if (!_connectionQueue.Writer.TryWrite(acceptSocket))
        {
            // too many pending connections
            acceptSocket.Close();
        }
    }

    private void BeginAccept(AcceptSocketAsyncEventArgs? socketAsyncEventArgs)
    {
        static void OnAcceptedSynchronous(object? state)
        {
            var acceptSocketAsyncEventArgs = (AcceptSocketAsyncEventArgs)state!;

            try
            {
                acceptSocketAsyncEventArgs.TunnelInformation!.OnAccepted(acceptSocketAsyncEventArgs);
            }
            catch (Exception)
            {
            }
        }

        socketAsyncEventArgs ??= SocketAsyncEventArgsPool.AcceptPool.Get();
        socketAsyncEventArgs.TunnelInformation = this;
        socketAsyncEventArgs.AcceptSocket = null; // reset

        if (!ServerSocket.AcceptAsync(socketAsyncEventArgs))
        {
            ThreadPool.QueueUserWorkItem(OnAcceptedSynchronous, socketAsyncEventArgs);
        }
    }

    public JsonObject CreateInformationJsonObject()
    {
        var localEndPoint = (IPEndPoint)ServerSocket.LocalEndPoint!;

        var jsonObject = new JsonObject
        {
            ["id"] = Id.Id,
            ["port"] = localEndPoint.Port,
            ["max_conn_count"] = MaximumConnections,
            ["url"] = $"https://{Id.Id}.{Id.Host}/",
        };

        return jsonObject;
    }

    private static HttpMethod AttemptMapToCachedMethod(string httpMethod) => httpMethod.ToUpperInvariant() switch
    {
        "DELETE" => HttpMethod.Delete,
        "GET" => HttpMethod.Get,
        "HEAD" => HttpMethod.Head,
        "OPTIONS" => HttpMethod.Options,
        "PATCH" => HttpMethod.Patch,
        "POST" => HttpMethod.Post,
        "PUT" => HttpMethod.Put,
        "TRACE" => HttpMethod.Trace,
        _ => new HttpMethod(httpMethod.ToUpperInvariant()),
    };

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (!disposing)
        {
            return;
        }

        _disposed = true;

        _tunnelService.TryUnregister(Id);

        _httpClient.Dispose();
        _connectionQueue.Writer.Complete();

        while (_connectionQueue.Reader.TryRead(out var socketConnection))
        {
            TryCloseSocket(socketConnection);
        }

        foreach (var (socketConnection, networkStream) in _openSockets)
        {
            networkStream.Close();
            TryCloseSocket(socketConnection);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private static bool TryCloseSocket(Socket socket)
    {
        try
        {
            socket.Close();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
