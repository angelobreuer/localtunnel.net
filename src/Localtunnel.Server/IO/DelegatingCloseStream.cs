namespace Localtunnel.Server.IO;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal sealed class DelegatingCloseStream : Stream
{
    private readonly Stream _stream;
    private readonly Action<object?> _postCleanupCallback;
    private readonly object? _callbackState;
    private bool _disposed;

    public DelegatingCloseStream(Stream stream!!, Action<object?> postCleanupCallback!!, object? state)
    {
        _stream = stream;
        _postCleanupCallback = postCleanupCallback;
        _callbackState = state;
    }

    /// <inheritdoc/>
    public override bool CanRead => _stream.CanRead;

    /// <inheritdoc/>
    public override bool CanSeek => _stream.CanSeek;

    /// <inheritdoc/>
    public override bool CanWrite => _stream.CanWrite;

    /// <inheritdoc/>
    public override long Length => _stream.Length;

    /// <inheritdoc/>
    public override long Position { get => _stream.Position; set => _stream.Position = value; }

    /// <inheritdoc/>
    public override bool CanTimeout => _stream.CanTimeout;

    /// <inheritdoc/>
    public override int ReadTimeout { get => _stream.ReadTimeout; set => _stream.ReadTimeout = value; }

    /// <inheritdoc/>
    public override int WriteTimeout { get => _stream.WriteTimeout; set => _stream.WriteTimeout = value; }

    /// <inheritdoc/>
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _stream.BeginRead(buffer, offset, count, callback, state);
    }

    /// <inheritdoc/>
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _stream.BeginWrite(buffer, offset, count, callback, state);
    }

    /// <inheritdoc/>
    public override void Close()
    {
        _stream.Close();
    }

    /// <inheritdoc/>
    public override void CopyTo(Stream destination, int bufferSize)
    {
        _stream.CopyTo(destination, bufferSize);
    }

    /// <inheritdoc/>
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return _stream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    /// <inheritdoc/>
    public override ValueTask DisposeAsync()
    {
        return _stream.DisposeAsync();
    }

    /// <inheritdoc/>
    public override int EndRead(IAsyncResult asyncResult)
    {
        return _stream.EndRead(asyncResult);
    }

    /// <inheritdoc/>
    public override void EndWrite(IAsyncResult asyncResult)
    {
        _stream.EndWrite(asyncResult);
    }

    /// <inheritdoc/>
    public override void Flush()
    {
        _stream.Flush();
    }

    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _stream.FlushAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        return _stream.Read(buffer, offset, count);
    }

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        return _stream.Read(buffer);
    }

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _stream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _stream.ReadAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        return _stream.ReadByte();
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        return _stream.Seek(offset, origin);
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        _stream.Write(buffer, offset, count);
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _stream.Write(buffer);
    }

    /// <inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _stream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _stream.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        _stream.WriteByte(value);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stream.Dispose();

        _postCleanupCallback(_callbackState);
    }
}
