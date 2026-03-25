using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Adapts a parameterless template to a parameterized template by ignoring the supplied parameter.
/// </summary>
/// <typeparam name="TParameter">The ignored parameter type.</typeparam>
public sealed class
    IgnoreParameterValidationErrorMessageTemplate<TParameter> : IValidationErrorMessageTemplate<TParameter>
{
    /// <summary>
    /// Initializes a new instance of <see cref="IgnoreParameterValidationErrorMessageTemplate{TParameter}" />.
    /// </summary>
    /// <param name="innerTemplate">The parameterless template to delegate to.</param>
    public IgnoreParameterValidationErrorMessageTemplate(IValidationErrorMessageTemplate innerTemplate) =>
        InnerTemplate = innerTemplate ?? throw new ArgumentNullException(nameof(innerTemplate));

    /// <summary>
    /// Gets the wrapped parameterless template.
    /// </summary>
    public IValidationErrorMessageTemplate InnerTemplate { get; }

    /// <inheritdoc />
    public ValidationErrorMessage ProvideMessage<T>(
        in ValidationErrorMessageContext<T> context,
        TParameter parameter
    ) => InnerTemplate.ProvideMessage(in context);
}
