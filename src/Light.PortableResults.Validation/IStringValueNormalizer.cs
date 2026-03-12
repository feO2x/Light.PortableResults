namespace Light.PortableResults.Validation;

/// <summary>
/// Normalizes string values before checks are materialized.
/// </summary>
public interface IStringValueNormalizer
{
    /// <summary>
    /// Normalizes the specified value.
    /// </summary>
    /// <param name="value">The original value.</param>
    /// <returns>The normalized value.</returns>
    string? Normalize(string? value);
}
