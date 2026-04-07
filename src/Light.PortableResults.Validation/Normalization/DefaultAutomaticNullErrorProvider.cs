using Light.PortableResults.Validation.Caching;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;
using Light.PortableResults.Validation.Targeting;

namespace Light.PortableResults.Validation.Normalization;

/// <summary>
/// Produces the default validation error for automatic null checks.
/// </summary>
public sealed class DefaultAutomaticNullErrorProvider : IAutomaticNullErrorProvider
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultAutomaticNullErrorProvider" />.
    /// </summary>
    public DefaultAutomaticNullErrorProvider() { }

    /// <summary>
    /// Gets the shared singleton instance.
    /// </summary>
    public static DefaultAutomaticNullErrorProvider Instance { get; } = new ();

    /// <inheritdoc />
    public bool TryCreateError<T>(in ValidationErrorMessageContext<T> context, out Error error)
    {
        var definition = BuiltInValidationErrorDefinitions.NotNull;
        var cache = context.ValidationContext.ErrorTemplates.MessageCache;
        ValidationErrorMessage message;

        if (
            cache is not null &&
            definition.TryGetStableMessageProvider(context.ValidationContext, out var provider)
        )
        {
            // The automatic null error provider is called with an already-resolved target, so we use
            // ValidationTarget.Absolute for the cache key. The target prefix is empty because the target
            // is pre-composed.
            var absoluteTarget = ValidationTarget.Absolute(context.Target, isNormalized: true);
            var key = new ValidationErrorMessageCacheKey(
                provider,
                absoluteTarget,
                string.Empty,
                context.DisplayName,
                context.ValidationContext.Options.CultureInfo
            );
            if (!cache.TryGet(key, out var entry))
            {
                message = definition.ProvideMessage(in context);
                cache.Store(key, new CachedValidationErrorMessage(message, context.Target));
            }
            else
            {
                message = entry.Message;
            }
        }
        else
        {
            message = definition.ProvideMessage(in context);
        }

        error = message.ToError(definition.Code, context.Target, definition.Category, definition.Metadata);
        return true;
    }
}
