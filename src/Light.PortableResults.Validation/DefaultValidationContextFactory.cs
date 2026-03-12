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
    public DefaultValidationContextFactory(ValidationContextOptions? options = null) =>
        Options = options ?? ValidationContextOptions.Default;

    /// <summary>
    /// Gets the options used for newly created validation contexts.
    /// </summary>
    public ValidationContextOptions Options { get; }

    /// <inheritdoc />
    public ValidationContext CreateValidationContext() => new (new ValidationState(Options), string.Empty);
}
