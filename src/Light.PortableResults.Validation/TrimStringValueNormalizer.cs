namespace Light.PortableResults.Validation;

/// <summary>
/// Trims incoming string values and converts <see langword="null" /> to an empty string.
/// </summary>
public sealed class TrimStringValueNormalizer : IStringValueNormalizer
{
    private TrimStringValueNormalizer() { }

    /// <summary>
    /// Gets the shared singleton instance.
    /// </summary>
    public static TrimStringValueNormalizer Instance { get; } = new ();

    /// <inheritdoc />
    public string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
