namespace Light.PortableResults.Validation;

/// <summary>
/// Leaves incoming string values unchanged.
/// </summary>
public sealed class NoOpStringValueNormalizer : IStringValueNormalizer
{
    private NoOpStringValueNormalizer() { }

    /// <summary>
    /// Gets the shared singleton instance.
    /// </summary>
    public static NoOpStringValueNormalizer Instance { get; } = new ();

    /// <inheritdoc />
    public string? Normalize(string? value) => value;
}
