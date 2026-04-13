namespace Light.PortableResults.Validation.Normalization;

/// <summary>
/// Provides discoverable entry points to the built-in value normalizer singletons.
/// </summary>
public static class ValueNormalizers
{
    /// <summary>
    /// Gets the trimming normalizer that trims whitespace from strings and converts <see langword="null" /> to empty.
    /// </summary>
    public static IValueNormalizer Trim { get; } = TrimStringNormalizer.Instance;

    /// <summary>
    /// Gets the no-op normalizer that leaves all values unchanged.
    /// </summary>
    public static IValueNormalizer NoOp { get; } = NoOpValueNormalizer.Instance;
}
