namespace Light.PortableResults.Validation;

/// <summary>
/// Creates validation errors for automatic null checks.
/// </summary>
public interface IAutomaticNullErrorProvider
{
    /// <summary>
    /// Tries to create an error for a null value.
    /// </summary>
    /// <typeparam name="T">The validated type.</typeparam>
    /// <param name="context">The readonly message context.</param>
    /// <param name="error">The created error when one should be produced.</param>
    /// <returns><see langword="true" /> when an error was created; otherwise, <see langword="false" />.</returns>
    bool TryCreateError<T>(in ValidationErrorMessageContext<T> context, out Error error);
}
