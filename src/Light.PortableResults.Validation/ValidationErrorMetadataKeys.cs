namespace Light.PortableResults.Validation;

/// <summary>
/// Defines metadata keys used by reusable validation error definitions.
/// </summary>
public static class ValidationErrorMetadataKeys
{
    /// <summary>
    /// Metadata key for single comparison boundaries.
    /// </summary>
    public const string ComparativeValue = "comparativeValue";

    /// <summary>
    /// Metadata key for inclusive lower boundaries.
    /// </summary>
    public const string LowerBoundary = "lowerBoundary";

    /// <summary>
    /// Metadata key for inclusive upper boundaries.
    /// </summary>
    public const string UpperBoundary = "upperBoundary";
}
