using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Normalizes raw validation target paths into the flat member path format used by Light.PortableResults.
/// </summary>
public interface IValidationTargetNormalizer
{
    /// <summary>
    /// Normalizes the specified raw target path.
    /// </summary>
    /// <param name="rawPath">The raw target path.</param>
    /// <returns>The normalized target path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rawPath" /> is <see langword="null" />.</exception>
    string Normalize(string rawPath);
}
