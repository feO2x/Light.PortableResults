namespace Light.PortableResults.Validation.Normalization;

/// <summary>
/// Normalizes values before checks are materialized.
/// </summary>
public interface IValueNormalizer
{
    /// <summary>
    /// Normalizes the specified value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The original value.</param>
    /// <returns>The normalized value.</returns>
    T Normalize<T>(T value);
}
