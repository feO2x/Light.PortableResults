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
        var message = context.ValidationContext.ErrorTemplates.NotNull.ProvideMessage(in context);
        error = message.ToError("NotNull", context.Target);
        return true;
    }
}
