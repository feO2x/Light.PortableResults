namespace Light.PortableResults.Validation.Normalization;

/// <summary>
/// Leaves incoming values unchanged.
/// </summary>
public sealed class NoOpValueNormalizer : IValueNormalizer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoOpValueNormalizer" /> class.
    /// </summary>
    public NoOpValueNormalizer() { }

    /// <summary>
    /// Gets the shared singleton instance.
    /// </summary>
    public static NoOpValueNormalizer Instance { get; } = new ();

    /// <inheritdoc />
    public T Normalize<T>(T value) => value;
}
