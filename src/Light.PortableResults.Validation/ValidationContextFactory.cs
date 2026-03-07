using System;
using System.Runtime.CompilerServices;

namespace Light.PortableResults.Validation;

/// <summary>
/// Default implementation of <see cref="IValidationContextFactory" />.
/// </summary>
public sealed class ValidationContextFactory : IValidationContextFactory
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationContextFactory" />.
    /// </summary>
    /// <param name="options">The context options to use.</param>
    /// <param name="errorTemplates">The error templates to use.</param>
    public ValidationContextFactory(
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
        new (this, Options, ErrorTemplates, new ValidationErrorSink(), string.Empty);

    /// <inheritdoc />
    public ValidationContext CreateChildValidationContext<T>(
        ValidationContext parent,
        T childValue,
        [CallerArgumentExpression("childValue")] string targetPrefix = ""
    ) => CreateChildValidationContext(parent, targetPrefix);

    /// <inheritdoc />
    public ValidationContext CreateChildValidationContext(
        ValidationContext parent,
        string targetPrefix,
        bool isTargetPrefixNormalized = false
    )
    {
        if (parent is null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        if (targetPrefix is null)
        {
            throw new ArgumentNullException(nameof(targetPrefix));
        }

        var normalizedPrefix = isTargetPrefixNormalized
            ? targetPrefix
            : parent.NormalizeTarget(targetPrefix);
        var composedPrefix = ValidationTargets.Compose(parent.TargetPrefix, normalizedPrefix);
        return new ValidationContext(this, parent.Options, parent.ErrorTemplates, parent.Sink, composedPrefix);
    }
}
