using System.Collections.Concurrent;

namespace Light.PortableResults.Validation;

/// <summary>
/// Default thread-safe cache for stable validation error messages.
/// </summary>
public sealed class DefaultValidationErrorMessageCache : IValidationErrorMessageCache
{
    private readonly ConcurrentDictionary<ValidationErrorMessageCacheKey, CachedValidationErrorMessage> _messages =
        new ();

    /// <summary>
    /// Gets the shared singleton cache instance.
    /// </summary>
    public static DefaultValidationErrorMessageCache Default { get; } = new ();

    /// <inheritdoc />
    public bool TryGet(ValidationErrorMessageCacheKey key, out CachedValidationErrorMessage entry) =>
        _messages.TryGetValue(key, out entry);

    /// <inheritdoc />
    public void Store(ValidationErrorMessageCacheKey key, CachedValidationErrorMessage entry) => _messages[key] = entry;
}
