namespace Light.PortableResults.Validation;

/// <summary>
/// Provides thread-safe storage for reusable validation error messages.
/// </summary>
public interface IValidationErrorMessageCache
{
    /// <summary>
    /// Tries to get a cached message for the specified key.
    /// </summary>
    /// <param name="key">The message cache key.</param>
    /// <param name="message">The cached message when present.</param>
    /// <returns><see langword="true" /> when a cached message was found; otherwise, <see langword="false" />.</returns>
    bool TryGet(ValidationErrorMessageCacheKey key, out ValidationErrorMessage message);

    /// <summary>
    /// Stores the specified message for the given key.
    /// </summary>
    /// <param name="key">The message cache key.</param>
    /// <param name="message">The produced message.</param>
    void Store(ValidationErrorMessageCacheKey key, ValidationErrorMessage message);
}
