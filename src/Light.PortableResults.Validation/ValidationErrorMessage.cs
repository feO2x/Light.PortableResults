using System;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.Validation;

/// <summary>
/// Represents a generated validation error message and optional machine-readable key.
/// </summary>
public readonly struct ValidationErrorMessage : IEquatable<ValidationErrorMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrorMessage" /> struct.
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <param name="key">The optional machine-readable message key.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="text" /> is empty or contains only white space.</exception>
    public ValidationErrorMessage(string text, string? key = null)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("The message text must not be empty or whitespace.", nameof(text));
        }

        Text = text;
        Key = key;
    }

    /// <summary>
    /// Gets the message text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the optional machine-readable message key.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// Creates an <see cref="Error" /> from this message.
    /// </summary>
    /// <param name="code">
    /// The optional error code. If this value is not present, the <see cref="Key" /> is used as the error code.
    /// </param>
    /// <param name="target">The error target.</param>
    /// <param name="category">The error category.</param>
    /// <param name="metadata">Optional existing metadata.</param>
    /// <returns>The created error.</returns>
    public Error ToError(
        string? code,
        string? target,
        ErrorCategory category = ErrorCategory.Validation,
        MetadataObject? metadata = null
    ) =>
        new ()
        {
            Message = Text,
            Code = code ?? Key,
            Target = target,
            Category = category,
            Metadata = metadata
        };

    /// <inheritdoc />
    public bool Equals(ValidationErrorMessage other) =>
        string.Equals(Text, other.Text, StringComparison.Ordinal) &&
        string.Equals(Key, other.Key, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValidationErrorMessage other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Text, Key);
}
