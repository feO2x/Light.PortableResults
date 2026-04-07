using System;

namespace Light.PortableResults.Validation.Messaging;

/// <summary>
/// Provides the readonly data required to generate a validation error message.
/// </summary>
/// <typeparam name="T">The type of the validated value.</typeparam>
public readonly struct ValidationErrorMessageContext<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrorMessageContext{T}" /> struct.
    /// </summary>
    /// <param name="validationContext">The readonly validation context.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="target">The normalized target.</param>
    /// <param name="value">The validated value.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="displayName" /> or <paramref name="target" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="validationContext" /> is the default instance.</exception>
    public ValidationErrorMessageContext(
        ReadOnlyValidationContext validationContext,
        string displayName,
        string target,
        T value
    )
    {
        if (validationContext.IsDefault)
        {
            throw new ArgumentException(
                "The validation context must not be the default instance.",
                nameof(validationContext)
            );
        }

        ValidationContext = validationContext;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Value = value;
    }

    /// <summary>
    /// Gets the readonly validation context.
    /// </summary>
    public ReadOnlyValidationContext ValidationContext { get; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the normalized target.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the validated value.
    /// </summary>
    public T Value { get; }
}
