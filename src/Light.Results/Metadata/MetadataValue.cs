using System;
using System.Globalization;

namespace Light.Results.Metadata;

/// <summary>
/// Represents a JSON-compatible metadata value. This is a discriminated union
/// that can hold null, boolean, int64, double, string, array, or object values.
/// </summary>
public readonly struct MetadataValue : IEquatable<MetadataValue>
{
    private readonly MetadataPayload _payload;

    public MetadataKind Kind { get; }

    private MetadataValue(MetadataKind kind, MetadataPayload payload)
    {
        Kind = kind;
        _payload = payload;
    }

    public static MetadataValue Null => new (MetadataKind.Null, default);

    public static MetadataValue FromBoolean(bool value) =>
        new (MetadataKind.Boolean, new MetadataPayload(value ? 1L : 0L));

    public static MetadataValue FromInt64(long value) =>
        new (MetadataKind.Int64, new MetadataPayload(value));

    public static MetadataValue FromDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException("NaN and Infinity are not allowed in metadata values.", nameof(value));
        }

        return new MetadataValue(MetadataKind.Double, new MetadataPayload(value));
    }

    public static MetadataValue FromString(string? value) =>
        value is null ? Null : new MetadataValue(MetadataKind.String, new MetadataPayload(value));

    public static MetadataValue FromDecimal(decimal value)
    {
        var str = value.ToString(CultureInfo.InvariantCulture);
        return new MetadataValue(MetadataKind.String, new MetadataPayload(str));
    }

    public static MetadataValue FromArray(MetadataArray array) =>
        new (MetadataKind.Array, new MetadataPayload(array.Data));

    public static MetadataValue FromObject(MetadataObject obj) =>
        new (MetadataKind.Object, new MetadataPayload(obj.Data));

    // Implicit conversions for ergonomics
    public static implicit operator MetadataValue(bool value) => FromBoolean(value);
    public static implicit operator MetadataValue(int value) => FromInt64(value);
    public static implicit operator MetadataValue(long value) => FromInt64(value);
    public static implicit operator MetadataValue(float value) => FromDouble(value);
    public static implicit operator MetadataValue(double value) => FromDouble(value);
    public static implicit operator MetadataValue(decimal value) => FromDecimal(value);
    public static implicit operator MetadataValue(string? value) => FromString(value);
    public static implicit operator MetadataValue(MetadataArray array) => FromArray(array);
    public static implicit operator MetadataValue(MetadataObject obj) => FromObject(obj);

    // Typed getters
    public bool IsNull => Kind == MetadataKind.Null;

    public bool TryGetBoolean(out bool value)
    {
        if (Kind == MetadataKind.Boolean)
        {
            value = _payload.Int64 != 0;
            return true;
        }

        value = false;
        return false;
    }

    public bool TryGetInt64(out long value)
    {
        if (Kind == MetadataKind.Int64)
        {
            value = _payload.Int64;
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryGetDouble(out double value)
    {
        if (Kind == MetadataKind.Double)
        {
            value = _payload.Float64;
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryGetString(out string? value)
    {
        if (Kind == MetadataKind.String)
        {
            value = (string?) _payload.Reference;
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetDecimal(out decimal value)
    {
        if (Kind == MetadataKind.String && _payload.Reference is string str)
        {
            return decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        if (Kind == MetadataKind.Double)
        {
            try
            {
                value = (decimal) _payload.Float64;
                return true;
            }
            catch (OverflowException)
            {
                value = 0;
                return false;
            }
        }

        if (Kind == MetadataKind.Int64)
        {
            value = _payload.Int64;
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryGetArray(out MetadataArray value)
    {
        if (Kind == MetadataKind.Array && _payload.Reference is MetadataArrayData data)
        {
            value = new MetadataArray(data);
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetObject(out MetadataObject value)
    {
        if (Kind == MetadataKind.Object && _payload.Reference is MetadataObjectData data)
        {
            value = new MetadataObject(data);
            return true;
        }

        value = default;
        return false;
    }

    public MetadataArray AsArray() =>
        TryGetArray(out var arr) ? arr : throw new InvalidOperationException($"Cannot convert {Kind} to Array.");

    public MetadataObject AsObject() =>
        TryGetObject(out var obj) ? obj : throw new InvalidOperationException($"Cannot convert {Kind} to Object.");

    public bool Equals(MetadataValue other)
    {
        if (Kind != other.Kind)
        {
            return false;
        }

        return Kind switch
        {
            MetadataKind.Null => true,
            MetadataKind.Boolean or MetadataKind.Int64 => _payload.Int64 == other._payload.Int64,
            MetadataKind.Double => _payload.Float64.Equals(other._payload.Float64),
            MetadataKind.String => string.Equals(
                (string?) _payload.Reference,
                (string?) other._payload.Reference,
                StringComparison.Ordinal
            ),
            MetadataKind.Array or MetadataKind.Object => ReferenceEquals(_payload.Reference, other._payload.Reference),
            _ => false
        };
    }

    public override bool Equals(object? obj) => obj is MetadataValue other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (int) Kind * 397;
            return Kind switch
            {
                MetadataKind.Null => hash,
                MetadataKind.Boolean or MetadataKind.Int64 => hash ^ _payload.Int64.GetHashCode(),
                MetadataKind.Double => hash ^ _payload.Float64.GetHashCode(),
                MetadataKind.String => hash ^ (_payload.Reference?.GetHashCode() ?? 0),
                MetadataKind.Array or MetadataKind.Object => hash ^ (_payload.Reference?.GetHashCode() ?? 0),
                _ => hash
            };
        }
    }

    public static bool operator ==(MetadataValue left, MetadataValue right) => left.Equals(right);
    public static bool operator !=(MetadataValue left, MetadataValue right) => !left.Equals(right);

    public override string ToString()
    {
        return Kind switch
        {
            MetadataKind.Null => "null",
            MetadataKind.Boolean => _payload.Int64 != 0 ? "true" : "false",
            MetadataKind.Int64 => _payload.Int64.ToString(CultureInfo.InvariantCulture),
            MetadataKind.Double => _payload.Float64.ToString(CultureInfo.InvariantCulture),
            MetadataKind.String => $"\"{_payload.Reference}\"",
            MetadataKind.Array => "[...]",
            MetadataKind.Object => "{...}",
            _ => "unknown"
        };
    }
}
