using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Normalizes raw caller-expression target paths into the flat member path format used by Light.PortableResults.
/// </summary>
public interface IValidationTargetNormalizer
{
    /// <summary>
    /// Normalizes the specified raw caller-expression path.
    /// </summary>
    /// <param name="rawPath">
    /// The raw caller-expression path to normalize, such as <c>dto.Address.ZipCode</c>.
    /// Already normalized absolute targets such as <c>address.zipCode</c> should usually be passed through unchanged
    /// by the caller instead of being normalized again.
    /// </param>
    /// <returns>The normalized target path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rawPath" /> is <see langword="null" />.</exception>
    string Normalize(string rawPath);
}
