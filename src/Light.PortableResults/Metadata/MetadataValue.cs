using System;
using System.Globalization;
using System.Xml;

namespace Light.PortableResults.Metadata;

#pragma warning disable CS8524 // Unnamed enum values intentionally throw SwitchExpressionException.

/// <summary>
/// Represents a JSON-compatible metadata value. This is a discriminated union whose payload, JSON shape,
/// and canonical text encoding are determined by <see cref="Kind" />.
/// </summary>
public readonly struct MetadataValue : IEquatable<MetadataValue>
{
    private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK";
    private const string DateTimeOffsetFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz";
    private const string DateOnlyFormat = "yyyy-MM-dd";
    private const string TimeOnlyFormat = "HH:mm:ss.FFFFFFF";

    /// <summary>
    /// Gets the default annotation for metadata values, which is <see cref="MetadataValueAnnotation.SerializeInBodies" />.
    /// </summary>
    public const MetadataValueAnnotation DefaultAnnotation = MetadataValueAnnotation.SerializeInBodies;

    private readonly MetadataPayload _payload;

    private MetadataValue(
        MetadataKind kind,
        MetadataPayload payload,
        MetadataValueAnnotation annotation = DefaultAnnotation
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
    /// Gets the JSON shape of the stored value.
    /// </summary>
    public MetadataJsonShape JsonShape => Kind.GetJsonShape();

    /// <summary>
    /// Gets the annotation that specifies where this value should be serialized.
    /// </summary>
    public MetadataValueAnnotation Annotation { get; }

    /// <summary>
    /// Gets a <see cref="MetadataValue" /> representing a null value.
    /// </summary>
    public static MetadataValue Null => new (MetadataKind.Null, default);

    /// <summary>
    /// Creates a null metadata value.
    /// </summary>
    public static MetadataValue FromNull(MetadataValueAnnotation annotation = DefaultAnnotation) =>
        new (MetadataKind.Null, default, annotation);

    /// <summary>
    /// Creates a metadata value from a Boolean.
    /// </summary>
    public static MetadataValue FromBoolean(
        bool value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        new (MetadataKind.Boolean, new MetadataPayload(value ? 1L : 0L), annotation);

    /// <summary>
    /// Creates a metadata value from a signed 64-bit integer.
    /// </summary>
    public static MetadataValue FromInt64(
        long value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        new (MetadataKind.Int64, new MetadataPayload(value), annotation);

    /// <summary>
    /// Creates a metadata value from a double-precision floating-point number.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    public static MetadataValue FromDouble(
        double value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    )
    {
        ValidateFinite(value, nameof(value));
        return new MetadataValue(MetadataKind.Double, new MetadataPayload(value), annotation);
    }

    /// <summary>
    /// Creates a metadata value from a string. A null string becomes a null metadata value.
    /// </summary>
    public static MetadataValue FromString(
        string? value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        value is null ?
            FromNull(annotation) :
            new MetadataValue(MetadataKind.String, new MetadataPayload(value), annotation);

    /// <summary>
    /// Creates a metadata value from a decimal number.
    /// </summary>
    /// <remarks>
    /// The decimal is boxed so that the reference slot remains at offset 8 and <see cref="MetadataValue" />
    /// remains 24 bytes on a 64-bit process.
    /// </remarks>
    public static MetadataValue FromDecimal(
        decimal value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        new (MetadataKind.Decimal, new MetadataPayload((object) value), annotation);

    /// <summary>
    /// Creates a metadata value from an unsigned 64-bit integer.
    /// </summary>
    public static MetadataValue FromUInt64(
        ulong value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        new (MetadataKind.UInt64, MetadataPayload.FromUInt64(value), annotation);

    /// <summary>
    /// Creates a metadata value from a single-precision floating-point number.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    public static MetadataValue FromSingle(
        float value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    )
    {
        ValidateFinite(value, nameof(value));
        return new MetadataValue(MetadataKind.Single, new MetadataPayload((double) value), annotation);
    }

    /// <summary>
    /// Creates a metadata value from a UTF-16 character.
    /// </summary>
    public static MetadataValue FromChar(
        char value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        new (MetadataKind.Char, new MetadataPayload(value), annotation);

    /// <summary>
    /// <para>
    /// Creates a metadata value from a date and time.
    /// </para>
    /// <para>
    /// <see cref="DateTimeKind.Local" /> values are converted to UTC, because a local wall clock is meaningless
    /// once it leaves the process. <see cref="DateTimeKind.Unspecified" /> values are stored as they are and
    /// render without a UTC designator: they carry no zone, and inventing one would assert an instant the caller
    /// never chose. Such a value is valid ISO 8601 but not RFC 3339 - see the remarks on
    /// <see cref="ToCanonicalString" />.
    /// </para>
    /// </summary>
    public static MetadataValue FromDateTime(
        DateTime value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    )
    {
        // ToBinary packs the kind into the two spare high bits of the tick count, so the Utc/Unspecified
        // distinction survives in the same 8-byte Int64 slot that raw ticks used to occupy.
        var storedValue = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
        return new MetadataValue(
            MetadataKind.DateTime,
            new MetadataPayload(storedValue.ToBinary()),
            annotation
        );
    }

    /// <summary>
    /// Creates a metadata value from a date and time with an offset.
    /// </summary>
    public static MetadataValue FromDateTimeOffset(
        DateTimeOffset value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        new (MetadataKind.DateTimeOffset, new MetadataPayload((object) value), annotation);

#if NET10_0_OR_GREATER
    /// <summary>
    /// Creates a metadata value from a date without a time.
    /// </summary>
    public static MetadataValue FromDateOnly(
        DateOnly value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        new (MetadataKind.DateOnly, new MetadataPayload(value.DayNumber), annotation);

    /// <summary>
    /// Creates a metadata value from a time without a date.
    /// </summary>
    public static MetadataValue FromTimeOnly(
        TimeOnly value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        new (MetadataKind.TimeOnly, new MetadataPayload(value.Ticks), annotation);
#endif

    /// <summary>
    /// Creates a metadata value from a duration.
    /// </summary>
    public static MetadataValue FromTimeSpan(
        TimeSpan value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        new (MetadataKind.TimeSpan, new MetadataPayload(value.Ticks), annotation);

    /// <summary>
    /// Creates a metadata value from a globally unique identifier.
    /// </summary>
    public static MetadataValue FromGuid(
        Guid value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        new (MetadataKind.Guid, new MetadataPayload((object) value), annotation);

    /// <summary>
    /// Creates a metadata value from a URI. A null URI becomes a null metadata value.
    /// </summary>
    public static MetadataValue FromUri(
        Uri? value,
        MetadataValueAnnotation annotation = DefaultAnnotation
    ) =>
        value is null ?
            FromNull(annotation) :
            new MetadataValue(MetadataKind.Uri, new MetadataPayload(value), annotation);

    /// <summary>
    /// Creates a metadata value from a metadata array.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the annotation cannot be applied to the array.</exception>
    public static MetadataValue FromArray(
        MetadataArray array,
        MetadataValueAnnotation annotation = DefaultAnnotation
    )
    {
        ValidateArrayAnnotation(array, annotation);
        return new MetadataValue(MetadataKind.Array, new MetadataPayload(array.Data), annotation);
    }

    /// <summary>
    /// Creates a metadata value from a metadata object.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the annotation cannot be applied to an object.</exception>
    public static MetadataValue FromObject(
        MetadataObject @object,
        MetadataValueAnnotation annotation = DefaultAnnotation
    )
    {
        if ((annotation & MetadataValueAnnotation.SerializeInHttpHeader) != 0)
        {
            throw new ArgumentException(
                "Objects cannot be serialized as HTTP headers. Use SerializeInHttpResponseBody instead.",
                nameof(annotation)
            );
        }

        if ((annotation & MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes) != 0)
        {
            throw new ArgumentException(
                "Objects cannot be serialized as CloudEvents extension attributes. Use SerializeInCloudEventsData instead.",
                nameof(annotation)
            );
        }

        return new MetadataValue(MetadataKind.Object, new MetadataPayload(@object.Data), annotation);
    }

    private static void ValidateArrayAnnotation(MetadataArray array, MetadataValueAnnotation annotation)
    {
        if ((annotation & MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes) != 0)
        {
            throw new ArgumentException(
                "Arrays cannot be serialized as CloudEvents extension attributes. Use SerializeInCloudEventsData instead.",
                nameof(annotation)
            );
        }

        if (!array.HasOnlyPrimitiveChildren &&
            (annotation & MetadataValueAnnotation.SerializeInHttpHeader) != 0)
        {
            throw new ArgumentException(
                "Arrays containing nested arrays or objects cannot be serialized as HTTP headers.",
                nameof(annotation)
            );
        }
    }

    /// <summary>Converts a Boolean to a metadata value.</summary>
    public static implicit operator MetadataValue(bool value) => FromBoolean(value);

    /// <summary>Converts a 32-bit signed integer to a metadata value.</summary>
    public static implicit operator MetadataValue(int value) => FromInt64(value);

    /// <summary>Converts an 8-bit unsigned integer to a metadata value.</summary>
    public static implicit operator MetadataValue(byte value) => FromInt64(value);

    /// <summary>Converts an 8-bit signed integer to a metadata value.</summary>
    public static implicit operator MetadataValue(sbyte value) => FromInt64(value);

    /// <summary>Converts a 16-bit signed integer to a metadata value.</summary>
    public static implicit operator MetadataValue(short value) => FromInt64(value);

    /// <summary>Converts a 16-bit unsigned integer to a metadata value.</summary>
    public static implicit operator MetadataValue(ushort value) => FromInt64(value);

    /// <summary>Converts a 32-bit unsigned integer to a metadata value.</summary>
    public static implicit operator MetadataValue(uint value) => FromInt64(value);

    /// <summary>Converts a 64-bit signed integer to a metadata value.</summary>
    public static implicit operator MetadataValue(long value) => FromInt64(value);

    /// <summary>Converts a 64-bit unsigned integer to a metadata value.</summary>
    public static implicit operator MetadataValue(ulong value) => FromUInt64(value);

    /// <summary>Converts a single-precision floating-point number to a metadata value.</summary>
    public static implicit operator MetadataValue(float value) => FromSingle(value);

    /// <summary>Converts a double-precision floating-point number to a metadata value.</summary>
    public static implicit operator MetadataValue(double value) => FromDouble(value);

    /// <summary>Converts a decimal number to a metadata value.</summary>
    public static implicit operator MetadataValue(decimal value) => FromDecimal(value);

    /// <summary>Converts a UTF-16 character to a metadata value.</summary>
    public static implicit operator MetadataValue(char value) => FromChar(value);

    /// <summary>Converts a string to a metadata value.</summary>
    public static implicit operator MetadataValue(string? value) => FromString(value);

    /// <summary>Converts a date and time to a metadata value.</summary>
    public static implicit operator MetadataValue(DateTime value) => FromDateTime(value);

    /// <summary>Converts a date and time with an offset to a metadata value.</summary>
    public static implicit operator MetadataValue(DateTimeOffset value) => FromDateTimeOffset(value);

#if NET10_0_OR_GREATER
    /// <summary>Converts a date without a time to a metadata value.</summary>
    public static implicit operator MetadataValue(DateOnly value) => FromDateOnly(value);

    /// <summary>Converts a time without a date to a metadata value.</summary>
    public static implicit operator MetadataValue(TimeOnly value) => FromTimeOnly(value);
#endif

    /// <summary>Converts a duration to a metadata value.</summary>
    public static implicit operator MetadataValue(TimeSpan value) => FromTimeSpan(value);

    /// <summary>Converts a globally unique identifier to a metadata value.</summary>
    public static implicit operator MetadataValue(Guid value) => FromGuid(value);

    /// <summary>Converts a URI to a metadata value.</summary>
    public static implicit operator MetadataValue(Uri? value) => FromUri(value);

    /// <summary>Converts a metadata array to a metadata value.</summary>
    public static implicit operator MetadataValue(MetadataArray array) => FromArray(array);

    /// <summary>Converts a metadata object to a metadata value.</summary>
    public static implicit operator MetadataValue(MetadataObject obj) => FromObject(obj);

    /// <summary>Gets whether this value represents null.</summary>
    public bool IsNull => Kind == MetadataKind.Null;

    /// <summary>Attempts to get the stored Boolean.</summary>
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

    /// <summary>Attempts to get the stored signed 64-bit integer.</summary>
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
    /// Attempts to get a double-precision floating-point number. Single values are widened without loss.
    /// </summary>
    public bool TryGetDouble(out double value)
    {
        if (Kind == MetadataKind.Double || Kind == MetadataKind.Single)
        {
            value = _payload.Float64;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Attempts to get the stored string.</summary>
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
    /// Attempts to get a decimal number. Signed integers, doubles, and invariant numeric strings are converted.
    /// </summary>
    public bool TryGetDecimal(out decimal value)
    {
        if (Kind == MetadataKind.Decimal && _payload.Reference is decimal decimalValue)
        {
            value = decimalValue;
            return true;
        }

        if (Kind == MetadataKind.String && _payload.Reference is string stringValue)
        {
            return decimal.TryParse(
                stringValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value
            );
        }

        if (Kind == MetadataKind.Int64)
        {
            value = _payload.Int64;
            return true;
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

        value = 0;
        return false;
    }

    /// <summary>Attempts to get an unsigned 64-bit integer or parse its canonical string encoding.</summary>
    public bool TryGetUInt64(out ulong value)
    {
        if (Kind == MetadataKind.UInt64)
        {
            value = _payload.UInt64;
            return true;
        }

        if (TryGetRawString(out var text) &&
            ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) &&
            string.Equals(FormatUInt64(value), text, StringComparison.Ordinal))
        {
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Attempts to get a single-precision number or parse its canonical string encoding.</summary>
    public bool TryGetSingle(out float value)
    {
        if (Kind == MetadataKind.Single)
        {
            value = (float) _payload.Float64;
            return true;
        }

        if (TryGetRawString(out var text) &&
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            !float.IsNaN(value) &&
            !float.IsInfinity(value) &&
            string.Equals(FormatSingle(value), text, StringComparison.Ordinal))
        {
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Attempts to get a character or parse its canonical single-character string encoding.</summary>
    public bool TryGetChar(out char value)
    {
        if (Kind == MetadataKind.Char)
        {
            value = (char) _payload.Int64;
            return true;
        }

        if (TryGetRawString(out var text) && text.Length == 1)
        {
            value = text[0];
            return true;
        }

        value = '\0';
        return false;
    }

    /// <summary>
    /// Attempts to get a UTC or unspecified date-time, or parse its canonical encoding. Text carrying a numeric
    /// UTC offset is rejected: that is the encoding of <see cref="MetadataKind.DateTimeOffset" />, and resolving
    /// it here would depend on the local time zone of the reading machine.
    /// </summary>
    public bool TryGetDateTime(out DateTime value)
    {
        if (Kind == MetadataKind.DateTime)
        {
            return TryReadStoredDateTime(_payload.Int64, out value);
        }

        if (TryGetRawString(out var text) &&
            DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out value
            ) &&
            value.Kind != DateTimeKind.Local &&
            string.Equals(FormatDateTime(value), text, StringComparison.Ordinal))
        {
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to get a date-time offset or parse its RFC 3339 encoding. Both designators for a zero offset
    /// are accepted - the canonical <c>+00:00</c> that this library writes and the <c>Z</c> that RFC 3339
    /// emitters commonly produce - so a value degraded to a string on the wire still reads back. Text without
    /// any offset is rejected, because it is not a point in time.
    /// </summary>
    public bool TryGetDateTimeOffset(out DateTimeOffset value)
    {
        if (Kind == MetadataKind.DateTimeOffset && _payload.Reference is DateTimeOffset dateTimeOffset)
        {
            value = dateTimeOffset;
            return true;
        }

        if (TryGetRawString(out var text) &&
            DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out value
            ) &&
            IsCanonicalDateTimeOffsetText(value, text))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static bool IsCanonicalDateTimeOffsetText(DateTimeOffset value, string text) =>
        string.Equals(FormatDateTimeOffset(value), text, StringComparison.Ordinal) ||
        // A zero offset is also written as "Z" by RFC 3339 emitters everywhere, including this library's own
        // DateTime kind, so it is accepted although the DateTimeOffset writer never produces it. Text without
        // any designator is still rejected: it parses against the reading machine's local offset and matches
        // neither form.
        (value.Offset == TimeSpan.Zero &&
         string.Equals(FormatDateTime(value.UtcDateTime), text, StringComparison.Ordinal));

#if NET10_0_OR_GREATER
    /// <summary>Attempts to get a date or parse its canonical RFC 3339 full-date encoding.</summary>
    public bool TryGetDateOnly(out DateOnly value)
    {
        if (Kind == MetadataKind.DateOnly)
        {
            try
            {
                value = DateOnly.FromDayNumber(checked((int) _payload.Int64));
                return true;
            }
            catch (Exception exception) when (exception is ArgumentOutOfRangeException or OverflowException)
            {
                value = default;
                return false;
            }
        }

        if (TryGetRawString(out var text) &&
            DateOnly.TryParseExact(
                text,
                DateOnlyFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out value
            ) &&
            string.Equals(FormatDateOnly(value.DayNumber), text, StringComparison.Ordinal))
        {
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Attempts to get a time or parse its canonical RFC 3339 full-time encoding.</summary>
    public bool TryGetTimeOnly(out TimeOnly value)
    {
        if (Kind == MetadataKind.TimeOnly)
        {
            try
            {
                value = new TimeOnly(_payload.Int64);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                value = default;
                return false;
            }
        }

        if (TryGetRawString(out var text) &&
            TimeOnly.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out value
            ) &&
            string.Equals(FormatTimeOnly(value.Ticks), text, StringComparison.Ordinal))
        {
            return true;
        }

        value = default;
        return false;
    }
#endif

    /// <summary>Attempts to get a duration or parse its canonical ISO 8601 encoding.</summary>
    public bool TryGetTimeSpan(out TimeSpan value)
    {
        if (Kind == MetadataKind.TimeSpan)
        {
            value = new TimeSpan(_payload.Int64);
            return true;
        }

        if (TryGetRawString(out var text))
        {
            try
            {
                value = XmlConvert.ToTimeSpan(text);
                if (string.Equals(FormatTimeSpan(value), text, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            catch (FormatException)
            {
                // The common false path returns below.
            }
        }

        value = TimeSpan.Zero;
        return false;
    }

    /// <summary>Attempts to get a GUID or parse its canonical lowercase RFC 4122 encoding.</summary>
    public bool TryGetGuid(out Guid value)
    {
        if (Kind == MetadataKind.Guid && _payload.Reference is Guid guid)
        {
            value = guid;
            return true;
        }

        if (TryGetRawString(out var text) &&
            Guid.TryParseExact(text, "D", out value) &&
            string.Equals(FormatGuid(value), text, StringComparison.Ordinal))
        {
            return true;
        }

        value = Guid.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to get a URI. String values are accepted only when they are absolute URI canonical encodings.
    /// </summary>
    public bool TryGetUri(out Uri? value)
    {
        if (Kind == MetadataKind.Uri && _payload.Reference is Uri uri)
        {
            value = uri;
            return true;
        }

        if (TryGetRawString(out var text) &&
            Uri.TryCreate(text, UriKind.Absolute, out value) &&
            string.Equals(value.OriginalString, text, StringComparison.Ordinal))
        {
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Attempts to get a metadata array.</summary>
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

    /// <summary>Attempts to get a metadata object.</summary>
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

    /// <summary>Gets the value as a metadata array.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the value is not an array.</exception>
    public MetadataArray AsArray() =>
        TryGetArray(out var array) ?
            array :
            throw new InvalidOperationException($"Cannot convert {Kind} to Array.");

    /// <summary>Gets the value as a metadata object.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the value is not an object.</exception>
    public MetadataObject AsObject() =>
        TryGetObject(out var @object) ?
            @object :
            throw new InvalidOperationException($"Cannot convert {Kind} to Object.");

    /// <summary>
    /// Returns the canonical text encoding of a primitive metadata value.
    /// </summary>
    /// <remarks>
    /// A <see cref="MetadataKind.DateTime" /> holding a <see cref="DateTimeKind.Unspecified" /> value is encoded
    /// without a UTC designator, for example <c>2026-07-26T13:45:30</c>. That form is valid ISO 8601 local time
    /// but not RFC 3339, which makes the offset mandatory, so it does not satisfy the OpenAPI
    /// <c>format: date-time</c> that this library generates for <see cref="System.DateTime" />. Prefer
    /// <see cref="DateTimeKind.Utc" /> or <see cref="System.DateTimeOffset" /> at API boundaries.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown for an array, object, or malformed payload.
    /// </exception>
    public string ToCanonicalString() =>
        Kind switch
        {
            MetadataKind.Null => "null",
            MetadataKind.Boolean => _payload.Int64 != 0 ? "true" : "false",
            MetadataKind.Int64 => _payload.Int64.ToString(CultureInfo.InvariantCulture),
            MetadataKind.Double => FormatDouble(_payload.Float64),
            MetadataKind.String => GetRequiredReference<string>(),
            MetadataKind.Decimal => GetRequiredReference<decimal>().ToString(CultureInfo.InvariantCulture),
            MetadataKind.UInt64 => FormatUInt64(_payload.UInt64),
            MetadataKind.Single => FormatSingle((float) _payload.Float64),
            MetadataKind.Char => ((char) _payload.Int64).ToString(),
            MetadataKind.DateTime => FormatStoredDateTime(_payload.Int64),
            MetadataKind.DateTimeOffset => FormatDateTimeOffset(GetRequiredReference<DateTimeOffset>()),
            MetadataKind.DateOnly => FormatDateOnly(_payload.Int64),
            MetadataKind.TimeOnly => FormatTimeOnly(_payload.Int64),
            MetadataKind.TimeSpan => FormatTimeSpan(new TimeSpan(_payload.Int64)),
            MetadataKind.Guid => FormatGuid(GetRequiredReference<Guid>()),
            MetadataKind.Uri => GetRequiredReference<Uri>().OriginalString,
            MetadataKind.Array => ThrowComplexCanonicalText(MetadataKind.Array),
            MetadataKind.Object => ThrowComplexCanonicalText(MetadataKind.Object)
        };

    /// <summary>
    /// Attempts to write the canonical text encoding to the supplied destination.
    /// </summary>
    /// <param name="destination">The destination for the encoded characters.</param>
    /// <param name="charsWritten">The number of characters written.</param>
    /// <returns><see langword="true" /> when the destination was large enough; otherwise, <see langword="false" />.</returns>
    public bool TryFormatCanonical(Span<char> destination, out int charsWritten)
    {
        var canonicalText = ToCanonicalString();
        if (canonicalText.AsSpan().TryCopyTo(destination))
        {
            charsWritten = canonicalText.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    internal MetadataValue WithAnnotation(MetadataValueAnnotation annotation) =>
        new (Kind, _payload, annotation);

    /// <inheritdoc />
    public bool Equals(MetadataValue other)
    {
        if (Kind != other.Kind)
        {
            return false;
        }

        return Kind switch
        {
            MetadataKind.Null => true,
            MetadataKind.Boolean => _payload.Int64 == other._payload.Int64,
            MetadataKind.Int64 => _payload.Int64 == other._payload.Int64,
            MetadataKind.Double => _payload.Float64.Equals(other._payload.Float64),
            MetadataKind.String => string.Equals(
                (string?) _payload.Reference,
                (string?) other._payload.Reference,
                StringComparison.Ordinal
            ),
            MetadataKind.Decimal => BoxedEquals<decimal>(other),
            MetadataKind.UInt64 => _payload.UInt64 == other._payload.UInt64,
            MetadataKind.Single => ((float) _payload.Float64).Equals((float) other._payload.Float64),
            MetadataKind.Char => _payload.Int64 == other._payload.Int64,
            MetadataKind.DateTime => _payload.Int64 == other._payload.Int64,
            MetadataKind.DateTimeOffset => BoxedEquals<DateTimeOffset>(other),
            MetadataKind.DateOnly => _payload.Int64 == other._payload.Int64,
            MetadataKind.TimeOnly => _payload.Int64 == other._payload.Int64,
            MetadataKind.TimeSpan => _payload.Int64 == other._payload.Int64,
            MetadataKind.Guid => BoxedEquals<Guid>(other),
            MetadataKind.Uri => UriEquals(other),
            MetadataKind.Array => ((MetadataArrayData?) _payload.Reference)?.Equals(
                                      (MetadataArrayData?) other._payload.Reference
                                  ) ??
                                  false,
            MetadataKind.Object => ((MetadataObjectData?) _payload.Reference)?.Equals(
                                       (MetadataObjectData?) other._payload.Reference
                                   ) ??
                                   false
        };
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MetadataValue other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var payloadHashCode = Kind switch
        {
            MetadataKind.Null => 0,
            MetadataKind.Boolean => _payload.Int64.GetHashCode(),
            MetadataKind.Int64 => _payload.Int64.GetHashCode(),
            MetadataKind.Double => _payload.Float64.GetHashCode(),
            MetadataKind.String => StringComparer.Ordinal.GetHashCode((string?) _payload.Reference ?? string.Empty),
            MetadataKind.Decimal => _payload.Reference is decimal decimalValue ? decimalValue.GetHashCode() : 0,
            MetadataKind.UInt64 => _payload.UInt64.GetHashCode(),
            MetadataKind.Single => ((float) _payload.Float64).GetHashCode(),
            MetadataKind.Char => ((char) _payload.Int64).GetHashCode(),
            MetadataKind.DateTime => _payload.Int64.GetHashCode(),
            MetadataKind.DateTimeOffset => _payload.Reference is DateTimeOffset dateTimeOffset ?
                dateTimeOffset.GetHashCode() :
                0,
            MetadataKind.DateOnly => _payload.Int64.GetHashCode(),
            MetadataKind.TimeOnly => _payload.Int64.GetHashCode(),
            MetadataKind.TimeSpan => _payload.Int64.GetHashCode(),
            MetadataKind.Guid => _payload.Reference is Guid guid ? guid.GetHashCode() : 0,
            MetadataKind.Uri => _payload.Reference is Uri uri ?
                StringComparer.Ordinal.GetHashCode(uri.OriginalString) :
                0,
            MetadataKind.Array => _payload.Reference?.GetHashCode() ?? 0,
            MetadataKind.Object => _payload.Reference?.GetHashCode() ?? 0
        };

        return HashCode.Combine(Kind, payloadHashCode);
    }

    /// <summary>Determines whether two metadata values are equal.</summary>
    public static bool operator ==(MetadataValue left, MetadataValue right) => left.Equals(right);

    /// <summary>Determines whether two metadata values are unequal.</summary>
    public static bool operator !=(MetadataValue left, MetadataValue right) => !left.Equals(right);

    /// <summary>
    /// Returns a debug-oriented representation of the metadata value.
    /// </summary>
    public override string ToString() =>
        Kind switch
        {
            MetadataKind.Null => ToCanonicalString(),
            MetadataKind.Boolean => ToCanonicalString(),
            MetadataKind.Int64 => ToCanonicalString(),
            MetadataKind.Double => ToCanonicalString(),
            MetadataKind.String => "\"" + ToCanonicalString() + "\"",
            MetadataKind.Decimal => ToCanonicalString(),
            MetadataKind.UInt64 => "\"" + ToCanonicalString() + "\"",
            MetadataKind.Single => ToCanonicalString(),
            MetadataKind.Char => "\"" + ToCanonicalString() + "\"",
            MetadataKind.DateTime => "\"" + ToCanonicalString() + "\"",
            MetadataKind.DateTimeOffset => "\"" + ToCanonicalString() + "\"",
            MetadataKind.DateOnly => "\"" + ToCanonicalString() + "\"",
            MetadataKind.TimeOnly => "\"" + ToCanonicalString() + "\"",
            MetadataKind.TimeSpan => "\"" + ToCanonicalString() + "\"",
            MetadataKind.Guid => "\"" + ToCanonicalString() + "\"",
            MetadataKind.Uri => "\"" + ToCanonicalString() + "\"",
            MetadataKind.Array =>
                ((MetadataArrayData?) _payload.Reference)?.ToString() ??
                MetadataArray.EmptyArrayStringRepresentation,
            MetadataKind.Object => "{...}"
        };

    private bool TryGetRawString(out string value)
    {
        if (Kind == MetadataKind.String && _payload.Reference is string text)
        {
            value = text;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private T GetRequiredReference<T>()
    {
        if (_payload.Reference is T value)
        {
            return value;
        }

        throw new InvalidOperationException($"Kind '{Kind}' has an invalid payload.");
    }

    private bool BoxedEquals<T>(MetadataValue other)
        where T : struct, IEquatable<T> =>
        _payload.Reference is T value &&
        other._payload.Reference is T otherValue &&
        value.Equals(otherValue);

    private bool UriEquals(MetadataValue other) =>
        _payload.Reference is Uri uri &&
        other._payload.Reference is Uri otherUri &&
        string.Equals(uri.OriginalString, otherUri.OriginalString, StringComparison.Ordinal);

    private static void ValidateFinite(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException("NaN and Infinity are not allowed in metadata values.", parameterName);
        }
    }

    private static string FormatUInt64(ulong value) => value.ToString(CultureInfo.InvariantCulture);

    private static string FormatSingle(float value) =>
        EnsureFloatingPointMarker(value.ToString("R", CultureInfo.InvariantCulture));

    private static string FormatDouble(double value) =>
        EnsureFloatingPointMarker(value.ToString("R", CultureInfo.InvariantCulture));

    private static string EnsureFloatingPointMarker(string value) =>
        value.IndexOf('.') < 0 && value.IndexOf('E') < 0 && value.IndexOf('e') < 0 ?
            value + ".0" :
            value;

    private static bool TryReadStoredDateTime(long binaryValue, out DateTime value)
    {
        try
        {
            var storedValue = DateTime.FromBinary(binaryValue);

            // FromBinary resolves a Local payload against the time zone of the reading machine. FromDateTime
            // never stores one, so seeing it here means the payload is corrupt rather than merely out of range.
            if (storedValue.Kind == DateTimeKind.Local)
            {
                value = default;
                return false;
            }

            value = storedValue;
            return true;
        }
        catch (ArgumentException)
        {
            value = default;
            return false;
        }
    }

    private static string FormatStoredDateTime(long binaryValue) =>
        TryReadStoredDateTime(binaryValue, out var value) ?
            FormatDateTime(value) :
            throw new InvalidOperationException("The DateTime metadata payload is invalid.");

    private static string FormatDateTime(DateTime value) =>
        value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

    private static string FormatDateTimeOffset(DateTimeOffset value) =>
        value.ToString(DateTimeOffsetFormat, CultureInfo.InvariantCulture);

    private static string FormatDateOnly(long dayNumber)
    {
        try
        {
            var ticks = checked(dayNumber * TimeSpan.TicksPerDay);
            return new DateTime(ticks, DateTimeKind.Unspecified).ToString(
                DateOnlyFormat,
                CultureInfo.InvariantCulture
            );
        }
        catch (Exception exception) when (exception is ArgumentOutOfRangeException or OverflowException)
        {
            throw new InvalidOperationException("The DateOnly metadata payload is invalid.", exception);
        }
    }

    private static string FormatTimeOnly(long ticks)
    {
        if (ticks < 0 || ticks >= TimeSpan.TicksPerDay)
        {
            throw new InvalidOperationException("The TimeOnly metadata payload is invalid.");
        }

        return new DateTime(ticks, DateTimeKind.Unspecified).ToString(TimeOnlyFormat, CultureInfo.InvariantCulture);
    }

    private static string FormatTimeSpan(TimeSpan value) => XmlConvert.ToString(value);

    private static string FormatGuid(Guid value) => value.ToString("D", CultureInfo.InvariantCulture).ToLowerInvariant();

    private static string ThrowComplexCanonicalText(MetadataKind kind) =>
        throw new InvalidOperationException($"Kind '{kind}' does not have a primitive canonical text encoding.");
}

#pragma warning restore CS8524
