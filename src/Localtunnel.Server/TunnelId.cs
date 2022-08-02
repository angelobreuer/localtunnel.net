namespace Localtunnel.Server;

using System;

public readonly struct TunnelId : IEquatable<TunnelId>
{
    public TunnelId(string host, string id)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(id);

        Host = host;
        Id = id;
    }

    public string Host { get; }

    public string Id { get; }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is TunnelId id && Equals(id);
    }

    /// <inheritdoc/>
    public bool Equals(TunnelId other)
    {
        return Host.Equals(other.Host, StringComparison.OrdinalIgnoreCase)
            && Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            value1: StringComparer.OrdinalIgnoreCase.GetHashCode(Host),
            value2: StringComparer.OrdinalIgnoreCase.GetHashCode(Id));
    }

    public static bool operator ==(TunnelId left, TunnelId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TunnelId left, TunnelId right)
    {
        return !(left == right);
    }

    public override string ToString() => string.Concat(Id, ".", Host);
}