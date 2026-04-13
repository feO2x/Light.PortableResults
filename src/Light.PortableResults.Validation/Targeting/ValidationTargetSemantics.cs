namespace Light.PortableResults.Validation.Targeting;

/// <summary>
/// Specifies how a validation target input string must be interpreted.
/// </summary>
public enum ValidationTargetSemantics
{
    /// <summary>
    /// The input is a caller-expression-style target that still needs caller-expression normalization.
    /// </summary>
    CallerExpression,

    /// <summary>
    /// The input is a target path relative to the current validation scope.
    /// </summary>
    Relative,

    /// <summary>
    /// The input is an absolute target path for the entire validation run.
    /// </summary>
    Absolute
}

/// <summary>
/// Provides helper methods for working with <see cref="ValidationTargetSemantics" /> values.
/// </summary>
public static class ValidationTargetSemanticsExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="ValidationTargetSemantics" /> value is a defined enum member.
    /// </summary>
    /// <param name="semantics">The semantics value to validate.</param>
    /// <returns><see langword="true" /> if <paramref name="semantics" /> is a defined value; otherwise, <see langword="false" />.</returns>
    public static bool IsDefined(this ValidationTargetSemantics semantics) =>
        semantics is ValidationTargetSemantics.CallerExpression or
                     ValidationTargetSemantics.Relative or
                     ValidationTargetSemantics.Absolute;
}
