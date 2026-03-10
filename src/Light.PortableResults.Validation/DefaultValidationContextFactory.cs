namespace Light.PortableResults.Validation;

/// <summary>
/// Default implementation of <see cref="IValidationContextFactory" />.
/// </summary>
public sealed class DefaultValidationContextFactory : IValidationContextFactory
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultValidationContextFactory" />.
    /// </summary>
    /// <param name="options">The context options to use.</param>
    /// <param name="errorTemplates">The error templates to use.</param>
    public DefaultValidationContextFactory(
        ValidationContextOptions? options = null,
        ValidationErrorTemplates? errorTemplates = null
    )
    {
        Options = options ?? ValidationContextOptions.Default;
        ErrorTemplates = errorTemplates ?? ValidationErrorTemplates.Default;
    }

    /// <summary>
    /// Gets the options used for newly created validation contexts.
    /// </summary>
    public ValidationContextOptions Options { get; }

    /// <summary>
    /// Gets the error templates used for newly created validation contexts.
    /// </summary>
    public ValidationErrorTemplates ErrorTemplates { get; }

    /// <inheritdoc />
    public ValidationContext CreateValidationContext() =>
        new (new ValidationState(Options, ErrorTemplates), string.Empty);
}
