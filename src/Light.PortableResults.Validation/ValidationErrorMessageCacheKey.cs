using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Light.PortableResults.Validation;

/// <summary>
/// Identifies a cached validation error message by provider, target descriptor, target prefix, display name, and
/// culture.
/// </summary>
public readonly record struct ValidationErrorMessageCacheKey
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationErrorMessageCacheKey" />.
    /// </summary>
    /// <param name="provider">The message provider.</param>
    /// <param name="target">The validation target descriptor.</param>
    /// <param name="targetPrefix">The target prefix of the validation scope.</param>
    /// <param name="displayName">The display name, or <see langword="null" /> when derived from the resolved target.</param>
    /// <param name="culture">The culture used to format the message.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" />, <paramref name="targetPrefix" />, or <paramref name="culture" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="target" /> is the default instance.
    /// </exception>
    public ValidationErrorMessageCacheKey(
        object provider,
        ValidationTarget target,
        string targetPrefix,
        string? displayName,
        CultureInfo culture
    )
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        if (target.IsDefault)
        {
            throw new ArgumentException("The validation target must not be the default instance.", nameof(target));
        }

        Target = target;
        TargetPrefix = targetPrefix ?? throw new ArgumentNullException(nameof(targetPrefix));
        DisplayName = displayName;
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
    }

    /// <summary>
    /// Gets the message provider whose reference identity distinguishes one rule from another.
    /// </summary>
    public object Provider { get; }

    /// <summary>
    /// Gets the validation target descriptor.
    /// </summary>
    public ValidationTarget Target { get; }

    /// <summary>
    /// Gets the target prefix of the validation scope.
    /// </summary>
    public string TargetPrefix { get; }

    /// <summary>
    /// Gets the display name, or <see langword="null" /> when derived from the resolved target.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the culture used to format the message.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <inheritdoc />
    public bool Equals(ValidationErrorMessageCacheKey other) =>
        ReferenceEquals(Provider, other.Provider) &&
        Target.Equals(other.Target) &&
        string.Equals(TargetPrefix, other.TargetPrefix, StringComparison.Ordinal) &&
        string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal) &&
        ReferenceEquals(Culture, other.Culture);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(
            RuntimeHelpers.GetHashCode(Provider),
            Target,
            TargetPrefix,
            DisplayName,
            RuntimeHelpers.GetHashCode(Culture)
        );
}
