using System;

namespace Light.PortableResults.Validation;

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

        var culture = context.Options.CultureInfo;
        return value is IFormattable formattable ?
            formattable.ToString(null, culture) :
            value.ToString() ?? string.Empty;
    }
}
