namespace Light.PortableResults.Validation;

/// <summary>
/// Creates validation error messages for comparison rules with a single typed boundary.
/// </summary>
public interface IComparableValidationErrorMessageTemplate
{
    /// <summary>
    /// Creates a message for the specified context and comparison boundary.
    /// </summary>
    /// <typeparam name="T">The checked value type.</typeparam>
    /// <typeparam name="TParameter">The boundary type.</typeparam>
    /// <param name="context">The message context.</param>
    /// <param name="parameter">The comparison boundary.</param>
    /// <returns>The produced message.</returns>
    ValidationErrorMessage ProvideMessage<T, TParameter>(
        in ValidationErrorMessageContext<T> context,
        TParameter parameter
    );
}
