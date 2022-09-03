namespace Localtunnel.Cli;

using System;

public readonly struct StreamSnapshot<T>
{
    public StreamSnapshot(ReadOnlyMemory<byte> capturedData, long totalLength)
    {
        CapturedData = capturedData;
        TotalLength = totalLength;
    }

    public ReadOnlyMemory<byte> CapturedData { get; }

    public long TotalLength { get; }
}
