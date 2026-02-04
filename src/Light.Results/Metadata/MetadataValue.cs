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

    private MetadataValue(
        MetadataKind kind,
        MetadataPayload payload,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    )
    {
        Kind = kind;
        _payload = payload;
        Annotation = annotation;
    }

    /// <summary>
    /// Gets the kind of value stored in this instance.
    /// </summary>
    public MetadataKind Kind { get; }

    /// <summary>
    /// Gets the annotation that specifies where this value should be serialized in HTTP responses.
    /// </summary>
    public MetadataValueAnnotation Annotation { get; }

    /// <summary>
    /// Gets a <see cref="MetadataValue" /> representing a null value.
    /// </summary>
    public static MetadataValue Null => new (MetadataKind.Null, default);

    /// <summary>
    /// Creates a <see cref="MetadataValue" /> from a boolean.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="annotation">The serialization annotation.</param>
    /// <returns>The metadata value.</returns>
    public static MetadataValue FromBoolean(
        bool value,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    ) =>
        new (MetadataKind.Boolean, new MetadataPayload(value ? 1L : 0L), annotation);

    /// <summary>
    /// Creates a <see cref="MetadataValue" /> from an int64 value.
    /// </summary>
    /// <param name="value">The int64 value.</param>
    /// <param name="annotation">The serialization annotation.</param>
    /// <returns>The metadata value.</returns>
    public static MetadataValue FromInt64(
        long value,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    ) =>
        new (MetadataKind.Int64, new MetadataPayload(value), annotation);

    /// <summary>
    /// Creates a <see cref="MetadataValue" /> from a double value.
    /// </summary>
    /// <param name="value">The double value.</param>
    /// <param name="annotation">The serialization annotation.</param>
    /// <returns>The metadata value.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is NaN or Infinity.
    /// </exception>
    public static MetadataValue FromDouble(
        double value,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    )
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException("NaN and Infinity are not allowed in metadata values.", nameof(value));
        }

        return new MetadataValue(MetadataKind.Double, new MetadataPayload(value), annotation);
    }

    /// <summary>
    /// Creates a <see cref="MetadataValue" /> from a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="annotation">The serialization annotation.</param>
    /// <returns>The metadata value.</returns>
    public static MetadataValue FromString(
        string? value,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    ) =>
        value is null ? Null : new MetadataValue(MetadataKind.String, new MetadataPayload(value), annotation);

    /// <summary>
    /// Creates a <see cref="MetadataValue" /> from a decimal value.
    /// </summary>
    /// <param name="value">The decimal value.</param>
    /// <param name="annotation">The serialization annotation.</param>
    /// <returns>The metadata value.</returns>
    public static MetadataValue FromDecimal(
        decimal value,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    )
    {
        var @string = value.ToString(CultureInfo.InvariantCulture);
        return new MetadataValue(MetadataKind.String, new MetadataPayload(@string), annotation);
    }

    /// <summary>
    /// Creates a <see cref="MetadataValue" /> from a <see cref="MetadataArray" />.
    /// </summary>
    /// <param name="array">The array value.</param>
    /// <param name="annotation">The serialization annotation.</param>
    /// <returns>The metadata value.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="annotation" /> includes header serialization
    /// and <paramref name="array" /> contains non-primitive children.
    /// </exception>
    public static MetadataValue FromArray(
        MetadataArray array,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    )
    {
        ValidateArrayAnnotation(array, annotation);
        return new MetadataValue(MetadataKind.Array, new MetadataPayload(array.Data), annotation);
    }

    /// <summary>
    /// Creates a <see cref="MetadataValue" /> from a <see cref="MetadataObject" />.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <param name="annotation">The serialization annotation.</param>
    /// <returns>The metadata value.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="annotation" /> includes header serialization.
    /// </exception>
    public static MetadataValue FromObject(
        MetadataObject @object,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    )
    {
        if ((annotation & MetadataValueAnnotation.SerializeInHttpHeader) != 0)
        {
            throw new ArgumentException(
                "Objects cannot be serialized as HTTP headers. Use SerializeInHttpResponseBody instead.",
                nameof(annotation)
            );
        }

        return new MetadataValue(MetadataKind.Object, new MetadataPayload(@object.Data), annotation);
    }

    private static void ValidateArrayAnnotation(MetadataArray array, MetadataValueAnnotation annotation)
    {
        if ((annotation & MetadataValueAnnotation.SerializeInHttpHeader) == 0 || array.HasOnlyPrimitiveChildren)
        {
            return;
        }

        throw new ArgumentException(
            "Arrays containing nested arrays or objects cannot be serialized as HTTP headers.",
            nameof(annotation)
        );
    }

    // Implicit conversions for ergonomics
    /// <summary>
    /// Converts a boolean to a <see cref="MetadataValue" />.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>The metadata value.</returns>
    public static implicit operator MetadataValue(bool value) => FromBoolean(value);

    /// <summary>
    /// Converts an int32 to a <see cref="MetadataValue" />.
    /// </summary>
    /// <param name="value">The int32 value.</param>
    /// <returns>The metadata value.</returns>
    public static implicit operator MetadataValue(int value) => FromInt64(value);

    /// <summary>
    /// Converts an int64 to a <see cref="MetadataValue" />.
    /// </summary>
    /// <param name="value">The int64 value.</param>
    /// <returns>The metadata value.</returns>
    public static implicit operator MetadataValue(long value) => FromInt64(value);

    /// <summary>
    /// Converts a float to a <see cref="MetadataValue" />.
    /// </summary>
    /// <param name="value">The float value.</param>
    /// <returns>The metadata value.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is NaN or Infinity.
    /// </exception>
    public static implicit operator MetadataValue(float value) => FromDouble(value);

    /// <summary>
    /// Converts a double to a <see cref="MetadataValue" />.
    /// </summary>
    /// <param name="value">The double value.</param>
    /// <returns>The metadata value.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is NaN or Infinity.
    /// </exception>
    public static implicit operator MetadataValue(double value) => FromDouble(value);

    /// <summary>
    /// Converts a decimal to a <see cref="MetadataValue" />.
    /// </summary>
    /// <param name="value">The decimal value.</param>
    /// <returns>The metadata value.</returns>
    public static implicit operator MetadataValue(decimal value) => FromDecimal(value);

    /// <summary>
    /// Converts a string to a <see cref="MetadataValue" />.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The metadata value.</returns>
    public static implicit operator MetadataValue(string? value) => FromString(value);

    /// <summary>
    /// Converts a <see cref="MetadataArray" /> to a <see cref="MetadataValue" />.
    /// </summary>
    /// <param name="array">The array value.</param>
    /// <returns>The metadata value.</returns>
    public static implicit operator MetadataValue(MetadataArray array) => FromArray(array);

    /// <summary>
    /// Converts a <see cref="MetadataObject" /> to a <see cref="MetadataValue" />.
    /// </summary>
    /// <param name="obj">The object value.</param>
    /// <returns>The metadata value.</returns>
    public static implicit operator MetadataValue(MetadataObject obj) => FromObject(obj);

    // Typed getters
    /// <summary>
    /// Gets the value indicating whether this instance represents null.
    /// </summary>
    public bool IsNull => Kind == MetadataKind.Null;

    /// <summary>
    /// Attempts to get a boolean value.
    /// </summary>
    /// <param name="value">When this method returns, contains the boolean value if present.</param>
    /// <returns><see langword="true" /> if the value is a boolean; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Attempts to get an int64 value.
    /// </summary>
    /// <param name="value">When this method returns, contains the int64 value if present.</param>
    /// <returns><see langword="true" /> if the value is an int64; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Attempts to get a double value.
    /// </summary>
    /// <param name="value">When this method returns, contains the double value if present.</param>
    /// <returns><see langword="true" /> if the value is a double; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Attempts to get a string value.
    /// </summary>
    /// <param name="value">When this method returns, contains the string value if present.</param>
    /// <returns><see langword="true" /> if the value is a string; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Attempts to get a decimal value.
    /// </summary>
    /// <param name="value">When this method returns, contains the decimal value if present.</param>
    /// <returns><see langword="true" /> if the value can be represented as a decimal; otherwise, <see langword="false" />.</returns>
    public bool TryGetDecimal(out decimal value)
    {
        switch (Kind)
        {
            case MetadataKind.String when _payload.Reference is string @string:
                return decimal.TryParse(@string, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
            case MetadataKind.Double:
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

            case MetadataKind.Int64:
                value = _payload.Int64;
                return true;
            default:
                value = 0;
                return false;
        }
    }

    /// <summary>
    /// Attempts to get a <see cref="MetadataArray" /> value.
    /// </summary>
    /// <param name="value">When this method returns, contains the array value if present.</param>
    /// <returns><see langword="true" /> if the value is an array; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Attempts to get a <see cref="MetadataObject" /> value.
    /// </summary>
    /// <param name="value">When this method returns, contains the object value if present.</param>
    /// <returns><see langword="true" /> if the value is an object; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Gets the value as a <see cref="MetadataArray" />.
    /// </summary>
    /// <returns>The array value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is not an array.</exception>
    public MetadataArray AsArray() =>
        TryGetArray(out var arr) ? arr : throw new InvalidOperationException($"Cannot convert {Kind} to Array.");

    /// <summary>
    /// Gets the value as a <see cref="MetadataObject" />.
    /// </summary>
    /// <returns>The object value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is not an object.</exception>
    public MetadataObject AsObject() =>
        TryGetObject(out var obj) ? obj : throw new InvalidOperationException($"Cannot convert {Kind} to Object.");

    /// <summary>
    /// Determines whether this instance and another specified <see cref="MetadataValue" /> have the same value.
    /// </summary>
    /// <param name="other">The other value to compare.</param>
    /// <returns><see langword="true" /> if the values are equal; otherwise, <see langword="false" />.</returns>
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
            MetadataKind.Array => ((MetadataArrayData?) _payload.Reference)?.Equals(
                                      (MetadataArrayData?) other._payload.Reference
                                  ) ??
                                  false,
            MetadataKind.Object => ((MetadataObjectData?) _payload.Reference)?.Equals(
                                       (MetadataObjectData?) other._payload.Reference
                                   ) ??
                                   false,
            _ => false
        };
    }

    /// <summary>
    /// Determines whether this instance and a specified object have the same value.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> if the objects are equal; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj) => obj is MetadataValue other && Equals(other);

    /// <summary>
    /// Gets the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        var hashCodeBuilder = new HashCode();
        hashCodeBuilder.Add(Kind);
        switch (Kind)
        {
            case MetadataKind.Null:
                break;
            case MetadataKind.Boolean or MetadataKind.Int64:
                hashCodeBuilder.Add(_payload.Int64);
                break;
            case MetadataKind.Double:
                hashCodeBuilder.Add(_payload.Float64);
                break;
            case MetadataKind.String or MetadataKind.Array or MetadataKind.Object:
                hashCodeBuilder.Add(_payload.Reference?.GetHashCode() ?? 0);
                break;
        }

        return hashCodeBuilder.ToHashCode();
    }

    /// <summary>
    /// Determines whether two <see cref="MetadataValue" /> instances are equal.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns><see langword="true" /> if the instances are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(MetadataValue left, MetadataValue right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="MetadataValue" /> instances are not equal.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns><see langword="true" /> if the instances are not equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(MetadataValue left, MetadataValue right) => !left.Equals(right);

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The string representation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value kind is unknown.</exception>
    public override string ToString() =>
        Kind switch
        {
            MetadataKind.Null => "null",
            MetadataKind.Boolean => _payload.Int64 != 0 ? "true" : "false",
            MetadataKind.Int64 => _payload.Int64.ToString(CultureInfo.InvariantCulture),
            MetadataKind.Double => _payload.Float64.ToString(CultureInfo.InvariantCulture),
            MetadataKind.String => $"\"{_payload.Reference}\"",
            MetadataKind.Array =>
                ((MetadataArrayData?) _payload.Reference)?.ToString() ?? MetadataArray.EmptyArrayStringRepresentation,
            MetadataKind.Object => "{...}",
            _ => throw new InvalidOperationException($"Kind '{Kind}' is unknown")
        };
}
