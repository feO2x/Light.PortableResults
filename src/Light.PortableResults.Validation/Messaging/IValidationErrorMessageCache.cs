namespace Light.PortableResults.Validation.Messaging;

/// <summary>
/// Provides thread-safe storage for reusable validation error messages.
/// </summary>
public interface IValidationErrorMessageCache
{
    /// <summary>
    /// Tries to get a cached entry for the specified key.
    /// </summary>
    /// <param name="key">The message cache key.</param>
    /// <param name="entry">The cached entry when present.</param>
    /// <returns><see langword="true" /> when a cached entry was found; otherwise, <see langword="false" />.</returns>
    bool TryGet(ValidationErrorMessageCacheKey key, out CachedValidationErrorMessage entry);

    /// <summary>
    /// Stores the specified entry for the given key.
    /// </summary>
    /// <param name="key">The message cache key.</param>
    /// <param name="entry">The entry to cache.</param>
    void Store(ValidationErrorMessageCacheKey key, CachedValidationErrorMessage entry);
}
