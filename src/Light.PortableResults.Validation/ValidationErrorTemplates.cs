using System;
using System.Globalization;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides reusable format strings and formatting helpers for validation errors.
/// </summary>
public class ValidationErrorTemplates
{
    /// <summary>
    /// Gets the shared default templates instance.
    /// </summary>
    public static ValidationErrorTemplates Default { get; } = new ();

    /// <summary>
    /// Gets or sets the culture used when formatting parameters.
    /// </summary>
    public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Gets or sets the template for null-value validation failures.
    /// </summary>
    public string NotNull { get; set; } = "{0} must not be null";

    /// <summary>
    /// Gets or sets the template for empty or whitespace string validation failures.
    /// </summary>
    public string NotNullOrWhiteSpace { get; set; } = "{0} must not be empty";

    /// <summary>
    /// Gets or sets the template for minimum length validation failures.
    /// </summary>
    public string MinLength { get; set; } = "{0} must be at least {1} characters long";

    /// <summary>
    /// Gets or sets the template for maximum length validation failures.
    /// </summary>
    public string MaxLength { get; set; } = "{0} must be at most {1} characters long";

    /// <summary>
    /// Gets or sets the template for invalid pattern validation failures.
    /// </summary>
    public string Pattern { get; set; } = "{0} has an invalid format";

    /// <summary>
    /// Gets or sets the template for email validation failures.
    /// </summary>
    public string Email { get; set; } = "{0} must be an email address";

    /// <summary>
    /// Formats a parameter value using the configured culture.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted value.</returns>
    public virtual string FormatParameter<T>(T value) =>
        value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo),
            _ => value.ToString() ?? string.Empty
        };

    /// <summary>
    /// Formats the specified template with the configured culture.
    /// </summary>
    /// <param name="template">The template to format.</param>
    /// <param name="parameters">The parameters to inject into the template.</param>
    /// <returns>The formatted message.</returns>
    public virtual string Format(string template, params object?[] parameters)
    {
        if (template is null)
        {
            throw new ArgumentNullException(nameof(template));
        }

        return string.Format(CultureInfo, template, parameters);
    }
}
