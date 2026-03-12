using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Always returns the same validation error message.
/// </summary>
public sealed class ConstantValidationErrorMessageTemplate : IValidationErrorMessageTemplate
{
    private readonly ValidationErrorMessage _message;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConstantValidationErrorMessageTemplate" /> class.
    /// </summary>
    /// <param name="text">The constant message text.</param>
    /// <param name="key">The optional machine-readable message key.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="text" /> is empty or contains only white space.</exception>
    public ConstantValidationErrorMessageTemplate(string text, string? key = null) =>
        _message = new ValidationErrorMessage(text, key);

    /// <inheritdoc />
    public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) => _message;
}
