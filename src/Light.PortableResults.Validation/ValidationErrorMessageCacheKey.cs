using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Light.PortableResults.Validation;

/// <summary>
/// Identifies a cached validation error message by provider, display name, and culture.
/// </summary>
public readonly record struct ValidationErrorMessageCacheKey
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationErrorMessageCacheKey" />.
    /// </summary>
    /// <param name="provider">The message provider.</param>
    /// <param name="displayName">The display name used to generate the message.</param>
    /// <param name="culture">The culture used to format the message.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" />, <paramref name="displayName" />, or <paramref name="culture" /> is
    /// <see langword="null" />.
    /// </exception>
    public ValidationErrorMessageCacheKey(object provider, string displayName, CultureInfo culture)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
    }

    /// <summary>
    /// Gets the message provider whose reference identity distinguishes one rule from another.
    /// </summary>
    public object Provider { get; }

    /// <summary>
    /// Gets the display name used during message creation.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the culture used to format the message.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <inheritdoc />
    public bool Equals(ValidationErrorMessageCacheKey other) =>
        ReferenceEquals(Provider, other.Provider) &&
        string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal) &&
        ReferenceEquals(Culture, other.Culture);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(
            RuntimeHelpers.GetHashCode(Provider),
            DisplayName,
            RuntimeHelpers.GetHashCode(Culture)
        );
}
