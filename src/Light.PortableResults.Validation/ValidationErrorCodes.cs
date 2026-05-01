namespace Light.PortableResults.Validation;

/// <summary>
/// Defines the built-in validation error codes emitted by Light.PortableResults.Validation.
/// </summary>
public static class ValidationErrorCodes
{
    /// <summary>Validation error code for exact-count failures.</summary>
    public const string Count = "Count";
    /// <summary>Validation error code for minimum-count failures.</summary>
    public const string MinCount = "MinCount";
    /// <summary>Validation error code for maximum-count failures.</summary>
    public const string MaxCount = "MaxCount";
    /// <summary>Validation error code for minimum-length failures.</summary>
    public const string MinLength = "MinLength";
    /// <summary>Validation error code for maximum-length failures.</summary>
    public const string MaxLength = "MaxLength";
    /// <summary>Validation error code for length-range failures.</summary>
    public const string LengthInRange = "LengthInRange";
    /// <summary>Validation error code for equality failures.</summary>
    public const string EqualTo = "EqualTo";
    /// <summary>Validation error code for inequality failures.</summary>
    public const string NotEqualTo = "NotEqualTo";
    /// <summary>Validation error code for greater-than failures.</summary>
    public const string GreaterThan = "GreaterThan";
    /// <summary>Validation error code for greater-than-or-equal failures.</summary>
    public const string GreaterThanOrEqualTo = "GreaterThanOrEqualTo";
    /// <summary>Validation error code for less-than failures.</summary>
    public const string LessThan = "LessThan";
    /// <summary>Validation error code for less-than-or-equal failures.</summary>
    public const string LessThanOrEqualTo = "LessThanOrEqualTo";
    /// <summary>Validation error code for inclusive range failures.</summary>
    public const string InRange = "InRange";
    /// <summary>Validation error code for outside-range failures.</summary>
    public const string NotInRange = "NotInRange";
    /// <summary>Validation error code for exclusive-range failures.</summary>
    public const string ExclusiveRange = "ExclusiveRange";
    /// <summary>Validation error code for regular-expression pattern failures.</summary>
    public const string Pattern = "Pattern";
    /// <summary>Validation error code for enum-value failures.</summary>
    public const string Enum = "Enum";
    /// <summary>Validation error code for enum-name failures.</summary>
    public const string EnumName = "EnumName";
    /// <summary>Validation error code for decimal precision-and-scale failures.</summary>
    public const string PrecisionScale = "PrecisionScale";
    /// <summary>Validation error code for null-value failures.</summary>
    public const string NotNull = "NotNull";
    /// <summary>Validation error code for must-be-null failures.</summary>
    public const string Null = "Null";
    /// <summary>Validation error code for not-empty failures.</summary>
    public const string NotEmpty = "NotEmpty";
    /// <summary>Validation error code for empty-value failures.</summary>
    public const string Empty = "Empty";
    /// <summary>Validation error code for null-or-empty-or-whitespace string failures.</summary>
    public const string NotNullOrWhiteSpace = "NotNullOrWhiteSpace";
    /// <summary>Validation error code for email-format failures.</summary>
    public const string Email = "Email";
    /// <summary>Validation error code for digits-only failures.</summary>
    public const string DigitsOnly = "DigitsOnly";
    /// <summary>Validation error code for letters-and-digits-only failures.</summary>
    public const string LettersAndDigitsOnly = "LettersAndDigitsOnly";
    /// <summary>Validation error code for predicate-based failures.</summary>
    public const string Predicate = "Predicate";
}
