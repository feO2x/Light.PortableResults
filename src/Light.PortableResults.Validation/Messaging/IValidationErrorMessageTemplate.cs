namespace Light.PortableResults.Validation.Messaging;

/// <summary>
/// Creates validation error messages without boxing value-type inputs by default.
/// </summary>
public interface IValidationErrorMessageTemplate
{
    /// <summary>
    /// Gets a value indicating whether the produced messages depend only on the display name and fixed parameters.
    /// </summary>
    bool IsMessageStable { get; }

    /// <summary>
    /// Creates a message for the specified context.
    /// </summary>
    /// <typeparam name="T">The validated value type.</typeparam>
    /// <param name="context">The message context.</param>
    /// <returns>The produced message.</returns>
    ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context);
}

/// <summary>
/// Creates validation error messages that require an additional typed parameter.
/// </summary>
/// <typeparam name="TParameter">The parameter type.</typeparam>
public interface IValidationErrorMessageTemplate<in TParameter>
{
    /// <summary>
    /// Gets a value indicating whether the produced messages depend only on the display name and fixed parameters.
    /// </summary>
    bool IsMessageStable { get; }

    /// <summary>
    /// Creates a message for the specified context and parameter.
    /// </summary>
    /// <typeparam name="T">The validated value type.</typeparam>
    /// <param name="context">The message context.</param>
    /// <param name="parameter">The additional typed parameter.</param>
    /// <returns>The produced message.</returns>
    ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context, TParameter parameter);
}
