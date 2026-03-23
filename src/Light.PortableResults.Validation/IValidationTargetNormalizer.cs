using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Normalizes validation targets into the flat member path format used by Light.PortableResults.
/// </summary>
public interface IValidationTargetNormalizer
{
    /// <summary>
    /// Normalizes the specified target path according to the provided semantics.
    /// </summary>
    /// <param name="rawPath">The raw target input to normalize.</param>
    /// <param name="semantics">The semantics used to interpret <paramref name="rawPath" />.</param>
    /// <returns>The normalized target path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rawPath" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="semantics" /> is not a supported <see cref="ValidationTargetSemantics" /> value.
    /// </exception>
    string Normalize(string rawPath, ValidationTargetSemantics semantics);
}
