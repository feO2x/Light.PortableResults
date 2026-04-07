using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Caching;

/// <summary>
/// Represents a cached validation error message together with the resolved absolute target string.
/// </summary>
public readonly record struct CachedValidationErrorMessage
{
    /// <summary>
    /// Initializes a new instance of <see cref="CachedValidationErrorMessage" />.
    /// </summary>
    /// <param name="message">The cached message.</param>
    /// <param name="resolvedTarget">The resolved absolute target string.</param>
    public CachedValidationErrorMessage(ValidationErrorMessage message, string resolvedTarget)
    {
        Message = message;
        ResolvedTarget = resolvedTarget;
    }

    /// <summary>
    /// Gets the cached message.
    /// </summary>
    public ValidationErrorMessage Message { get; }

    /// <summary>
    /// Gets the resolved absolute target string.
    /// </summary>
    public string ResolvedTarget { get; }
}
