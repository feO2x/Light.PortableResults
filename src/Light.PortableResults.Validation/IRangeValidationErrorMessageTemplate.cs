namespace Light.PortableResults.Validation;

/// <summary>
/// Creates validation error messages for rules with lower and upper boundaries.
/// </summary>
public interface IRangeValidationErrorMessageTemplate
{
    /// <summary>
    /// Creates a message for the specified context and range boundaries.
    /// </summary>
    /// <typeparam name="T">The checked value type.</typeparam>
    /// <typeparam name="TBoundary">The boundary type.</typeparam>
    /// <param name="context">The message context.</param>
    /// <param name="lowerBoundary">The inclusive lower boundary.</param>
    /// <param name="upperBoundary">The inclusive upper boundary.</param>
    /// <returns>The produced message.</returns>
    ValidationErrorMessage ProvideMessage<T, TBoundary>(
        in ValidationErrorMessageContext<T> context,
        TBoundary lowerBoundary,
        TBoundary upperBoundary
    );
}
