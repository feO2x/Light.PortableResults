using System;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.Validation.Messaging;

/// <summary>
/// Provides methods to format a parameter.
/// </summary>
public static class ValidationErrorMessageFormatting
{
    /// <summary>
    /// Formats a parameter value using the culture stored in the validation context when available.
    /// </summary>
    /// <typeparam name="T">The type of the parameter value.</typeparam>
    /// <param name="value">The parameter value to format.</param>
    /// <param name="context">The validation context that may provide culture information.</param>
    /// <returns>The formatted parameter value, or an empty string if the value is <c>null</c>.</returns>
    public static string FormatParameter<T>(T value, ReadOnlyValidationContext context)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (TryFormatCanonical(value, out var canonicalValue))
        {
            return canonicalValue;
        }

        var culture = context.Options.CultureInfo;
        return value is IFormattable formattable ?
            formattable.ToString(null, culture) :
            value.ToString() ?? string.Empty;
    }

    private static bool TryFormatCanonical<T>(T value, out string formattedValue)
    {
        MetadataValue metadataValue;
        switch (value)
        {
            case ulong uint64:
                metadataValue = MetadataValue.FromUInt64(uint64);
                break;
            // NaN and Infinity have no metadata kind - the factories reject them - so they fall through to
            // the culture-aware fallback path below.
            case float single when !float.IsNaN(single) && !float.IsInfinity(single):
                metadataValue = MetadataValue.FromSingle(single);
                break;
            case double @double when !double.IsNaN(@double) && !double.IsInfinity(@double):
                metadataValue = MetadataValue.FromDouble(@double);
                break;
            case char character:
                metadataValue = MetadataValue.FromChar(character);
                break;
            case DateTime dateTime:
                metadataValue = MetadataValue.FromDateTime(dateTime);
                break;
            case DateTimeOffset dateTimeOffset:
                metadataValue = MetadataValue.FromDateTimeOffset(dateTimeOffset);
                break;
#if NET10_0_OR_GREATER
            case DateOnly dateOnly:
                metadataValue = MetadataValue.FromDateOnly(dateOnly);
                break;
            case TimeOnly timeOnly:
                metadataValue = MetadataValue.FromTimeOnly(timeOnly);
                break;
#endif
            case TimeSpan timeSpan:
                metadataValue = MetadataValue.FromTimeSpan(timeSpan);
                break;
            case Guid guid:
                metadataValue = MetadataValue.FromGuid(guid);
                break;
            case Uri uri:
                metadataValue = MetadataValue.FromUri(uri);
                break;
            default:
                formattedValue = string.Empty;
                return false;
        }

        formattedValue = metadataValue.ToCanonicalString();
        return true;
    }
}
