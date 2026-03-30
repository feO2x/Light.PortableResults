namespace Light.PortableResults.Validation;

/// <summary>
/// Produces the default validation error for automatic null checks.
/// </summary>
public sealed class DefaultAutomaticNullErrorProvider : IAutomaticNullErrorProvider
{
    private DefaultAutomaticNullErrorProvider() { }

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

        if (definition.IsMessageStable && cache is not null)
        {
            var key = new ValidationErrorMessageCacheKey(
                definition,
                context.DisplayName,
                context.ValidationContext.Options.CultureInfo
            );
            if (!cache.TryGet(key, out message))
            {
                message = definition.ProvideMessage(in context);
                cache.Store(key, message);
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
