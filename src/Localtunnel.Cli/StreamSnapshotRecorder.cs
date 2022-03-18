namespace Localtunnel.Cli;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal sealed class StreamSnapshotRecorder : Stream
{
    private readonly byte[] _outCaptureBuffer;
    private readonly byte[] _inCaptureBuffer;
    private Stream? _requestStream;
    private Stream? _responseStream;
    private long _totalBytesOut;
    private long _totalBytesIn;
    private int _capturedBytesOut;
    private int _capturedBytesIn;

    public StreamSnapshotRecorder(int maximumSnapshotLength = 16 * 1024)
    {
        _inCaptureBuffer = GC.AllocateUninitializedArray<byte>(maximumSnapshotLength);
        _outCaptureBuffer = GC.AllocateUninitializedArray<byte>(maximumSnapshotLength);
    }

    public void Snapshot(out StreamSnapshot<byte> bodyIn, out StreamSnapshot<byte> bodyOut)
    {
        bodyIn = new StreamSnapshot<byte>(_inCaptureBuffer.AsMemory(0, _capturedBytesIn), _totalBytesIn);
        bodyOut = new StreamSnapshot<byte>(_outCaptureBuffer.AsMemory(0, _capturedBytesOut), _totalBytesOut);
    }

    public override bool CanRead => RequestStream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => RequestStream.CanWrite;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public Stream RequestStream
    {
        get => _requestStream ?? throw new InvalidOperationException("The request stream is not available.");
        set => _requestStream = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Stream ResponseStream
    {
        get => _responseStream ?? throw new InvalidOperationException("The response stream is not available.");
        set => _responseStream = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override void Flush()
    {
        RequestStream.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        CaptureOut(buffer);
        ResponseStream.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        CaptureOut(buffer.Span);
        return ResponseStream.WriteAsync(buffer, cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        var bytesRead = RequestStream.Read(buffer);
        CaptureIn(buffer[..bytesRead]);
        return bytesRead;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytesRead = await RequestStream
            .ReadAsync(buffer, cancellationToken)
            .ConfigureAwait(false);

        CaptureIn(buffer.Span[..bytesRead]);
        return bytesRead;
    }

    private void CaptureIn(ReadOnlySpan<byte> buffer)
    {
        _totalBytesIn += buffer.Length;

        var bytesToCopy = Math.Min(buffer.Length, _inCaptureBuffer.Length - _capturedBytesIn);

        if (bytesToCopy is 0)
        {
            return;
        }

        buffer[..bytesToCopy].CopyTo(_inCaptureBuffer.AsSpan(_capturedBytesIn));
        _capturedBytesIn += bytesToCopy;
    }

    private void CaptureOut(ReadOnlySpan<byte> buffer)
    {
        _totalBytesOut += buffer.Length;

        var bytesToCopy = Math.Min(buffer.Length, _outCaptureBuffer.Length - _capturedBytesOut);

        if (bytesToCopy is 0)
        {
            return;
        }

        buffer[..bytesToCopy].CopyTo(_outCaptureBuffer.AsSpan(_capturedBytesOut));
        _capturedBytesOut += bytesToCopy;
    }
}
