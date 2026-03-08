using System.Runtime.CompilerServices;

namespace Light.PortableResults.Validation;

/// <summary>
/// Creates validation contexts for root and scoped validation runs.
/// </summary>
public interface IValidationContextFactory
{
    /// <summary>
    /// Creates a new root validation context.
    /// </summary>
    ValidationContext CreateValidationContext();

    /// <summary>
    /// Creates a child validation context whose errors are added to the same flat error sink as the parent.
    /// </summary>
    /// <typeparam name="T">The type of the child value.</typeparam>
    /// <param name="parent">The parent validation context.</param>
    /// <param name="childValue">The child value whose caller expression identifies the child target prefix.</param>
    /// <param name="targetPrefix">The raw caller expression for the child value.</param>
    /// <returns>The child validation context.</returns>
    ValidationContext CreateChildValidationContext<T>(
        ValidationContext parent,
        T childValue,
        [CallerArgumentExpression("childValue")] string targetPrefix = ""
    );

    /// <summary>
    /// Creates a child validation context with an explicit target prefix.
    /// </summary>
    /// <param name="parent">The parent validation context.</param>
    /// <param name="targetPrefix">The target prefix to use for the child scope.</param>
    /// <param name="isTargetPrefixNormalized">
    /// <see langword="true" /> when <paramref name="targetPrefix" /> is already normalized; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>The child validation context.</returns>
    ValidationContext CreateChildValidationContext(
        ValidationContext parent,
        string targetPrefix,
        bool isTargetPrefixNormalized = false
    );
}
