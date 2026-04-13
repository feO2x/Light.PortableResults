using System;

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
    public DefaultValidationContextFactory(ValidationContextOptions options) =>
        Options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Gets the options used for newly created validation contexts.
    /// </summary>
    public ValidationContextOptions Options { get; }

    /// <inheritdoc />
    public ValidationContext CreateValidationContext() => new (new ValidationState(Options), string.Empty);

    /// <summary>
    /// Creates a new instance of <see cref="DefaultValidationContextFactory" />.
    /// </summary>
    /// <param name="options">
    /// The options for the context. When <c>null</c>, a new instance of <see cref="ValidationContextOptions" /> is used.
    /// </param>
    /// <returns>The created <see cref="DefaultValidationContextFactory" /> instance.</returns>
    public static DefaultValidationContextFactory Create(ValidationContextOptions? options = null)
    {
        options ??= new ValidationContextOptions();
        return new DefaultValidationContextFactory(options);
    }
}
