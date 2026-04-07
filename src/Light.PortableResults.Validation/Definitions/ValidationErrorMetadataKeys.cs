namespace Light.PortableResults.Validation.Definitions;

/// <summary>
/// Defines metadata keys used by reusable validation error definitions.
/// </summary>
public static class ValidationErrorMetadataKeys
{
    /// <summary>
    /// Metadata key for single comparison boundaries or expected comparison values.
    /// </summary>
    public const string ComparativeValue = "comparativeValue";

    /// <summary>
    /// Metadata key for lower boundaries.
    /// </summary>
    public const string LowerBoundary = "lowerBoundary";

    /// <summary>
    /// Metadata key for upper boundaries.
    /// </summary>
    public const string UpperBoundary = "upperBoundary";

    /// <summary>
    /// Metadata key for exact expected counts.
    /// </summary>
    public const string ExpectedCount = "expectedCount";

    /// <summary>
    /// Metadata key for minimum expected counts.
    /// </summary>
    public const string MinCount = "minCount";

    /// <summary>
    /// Metadata key for maximum expected counts.
    /// </summary>
    public const string MaxCount = "maxCount";

    /// <summary>
    /// Metadata key for minimum expected string lengths.
    /// </summary>
    public const string MinLength = "minLength";

    /// <summary>
    /// Metadata key for maximum expected string lengths.
    /// </summary>
    public const string MaxLength = "maxLength";

    /// <summary>
    /// Metadata key for regular-expression patterns.
    /// </summary>
    public const string Pattern = "pattern";

    /// <summary>
    /// Metadata key for regular-expression options.
    /// </summary>
    public const string RegexOptions = "regexOptions";

    /// <summary>
    /// Metadata key for enum type names.
    /// </summary>
    public const string EnumType = "enumType";

    /// <summary>
    /// Metadata key for enum-name case-sensitivity settings.
    /// </summary>
    public const string IgnoreCase = "ignoreCase";

    /// <summary>
    /// Metadata key for expected decimal precision values.
    /// </summary>
    public const string ExpectedPrecision = "expectedPrecision";

    /// <summary>
    /// Metadata key for expected decimal scale values.
    /// </summary>
    public const string ExpectedScale = "expectedScale";

    /// <summary>
    /// Metadata key for decimal trailing-zero behavior.
    /// </summary>
    public const string IgnoreTrailingZeros = "ignoreTrailingZeros";
}
