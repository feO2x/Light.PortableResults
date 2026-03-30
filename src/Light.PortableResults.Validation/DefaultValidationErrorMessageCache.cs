using System.Collections.Concurrent;

namespace Light.PortableResults.Validation;

/// <summary>
/// Default thread-safe cache for stable validation error messages.
/// </summary>
public sealed class DefaultValidationErrorMessageCache : IValidationErrorMessageCache
{
    private readonly ConcurrentDictionary<ValidationErrorMessageCacheKey, ValidationErrorMessage> _messages = new ();

    /// <summary>
    /// Gets the shared singleton cache instance.
    /// </summary>
    public static DefaultValidationErrorMessageCache Default { get; } = new ();

    /// <inheritdoc />
    public bool TryGet(ValidationErrorMessageCacheKey key, out ValidationErrorMessage message) =>
        _messages.TryGetValue(key, out message);

    /// <inheritdoc />
    public void Store(ValidationErrorMessageCacheKey key, ValidationErrorMessage message) => _messages[key] = message;
}
