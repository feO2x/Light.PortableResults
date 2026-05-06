using System;

namespace Light.PortableResults.Validation.Definitions;

/// <summary>
/// Describes how a validation rule's metadata contract is represented for compile-time documentation.
/// </summary>
public enum ValidationRuleMetadataShape
{
    /// <summary>
    /// The rule is documented through the globally registered or endpoint-inline metadata contract for its code.
    /// </summary>
    Registered = 0,

    /// <summary>
    /// The rule has one comparative metadata value whose CLR type is supplied by the validated check type.
    /// </summary>
    TypedComparison = 1,

    /// <summary>
    /// The rule has lower and upper boundary metadata values whose CLR type is supplied by the validated check type.
    /// </summary>
    TypedRange = 2
}

/// <summary>
/// Marks a validation check method as emitting a documentable validation error code.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ValidationRuleAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationRuleAttribute" />.
    /// </summary>
    public ValidationRuleAttribute(string errorCode)
    {
        ValidationAttributeGuard.EnsureNotNullOrWhiteSpace(errorCode, nameof(errorCode));
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationRuleAttribute" />.
    /// </summary>
    public ValidationRuleAttribute(string errorCode, ValidationRuleMetadataShape metadataShape)
        : this(errorCode) =>
        MetadataShape = metadataShape;

    /// <summary>
    /// Gets the emitted validation error code.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the metadata shape the source generator should use for this rule.
    /// </summary>
    public ValidationRuleMetadataShape MetadataShape { get; } = ValidationRuleMetadataShape.Registered;

    /// <summary>
    /// Gets or sets the validation error definition type that declares this rule's metadata contract.
    /// </summary>
    public Type? ErrorDefinitionType { get; set; }
}

/// <summary>
/// Binds one metadata property emitted by a validation rule to a check-method argument or a fixed value.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ValidationRuleMetadataAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationRuleMetadataAttribute" /> bound to fixed attribute values.
    /// </summary>
    public ValidationRuleMetadataAttribute(string metadataKey)
    {
        ValidationAttributeGuard.EnsureNotNullOrWhiteSpace(metadataKey, nameof(metadataKey));
        MetadataKey = metadataKey;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationRuleMetadataAttribute" /> bound to a source argument.
    /// </summary>
    public ValidationRuleMetadataAttribute(string metadataKey, string sourceArgument)
    {
        ValidationAttributeGuard.EnsureNotNullOrWhiteSpace(metadataKey, nameof(metadataKey));
        ValidationAttributeGuard.EnsureNotNullOrWhiteSpace(sourceArgument, nameof(sourceArgument));

        MetadataKey = metadataKey;
        SourceArgument = sourceArgument;
    }

    /// <summary>
    /// Gets the metadata key.
    /// </summary>
    public string MetadataKey { get; }

    /// <summary>
    /// Gets the check-method parameter name that supplies this metadata value.
    /// </summary>
    public string? SourceArgument { get; }

    /// <summary>
    /// Gets or sets a fixed string metadata value.
    /// </summary>
    public string? ConstantStringValue { get; set; }

    /// <summary>
    /// Gets or sets a fixed integer metadata value.
    /// </summary>
    public long ConstantInt64Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ConstantInt64Value" /> is set.
    /// </summary>
    public bool HasConstantInt64Value { get; set; }

    /// <summary>
    /// Gets or sets a fixed Boolean metadata value.
    /// </summary>
    public bool ConstantBooleanValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ConstantBooleanValue" /> is set.
    /// </summary>
    public bool HasConstantBooleanValue { get; set; }

    /// <summary>
    /// Gets or sets a fixed type metadata value.
    /// </summary>
    public Type? ConstantTypeValue { get; set; }
}

/// <summary>
/// Marks a validation error definition as having a stable documentable error contract.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ValidationErrorContractAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationErrorContractAttribute" />.
    /// </summary>
    public ValidationErrorContractAttribute(string errorCode)
    {
        ValidationAttributeGuard.EnsureNotNullOrWhiteSpace(errorCode, nameof(errorCode));
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the validation error code.
    /// </summary>
    public string ErrorCode { get; }
}

/// <summary>
/// Describes one metadata property exposed by a validation error contract.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ValidationErrorMetadataContractAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationErrorMetadataContractAttribute" />.
    /// </summary>
    public ValidationErrorMetadataContractAttribute(string metadataKey, Type metadataType)
    {
        ValidationAttributeGuard.EnsureNotNullOrWhiteSpace(metadataKey, nameof(metadataKey));
        if (metadataType is null)
        {
            throw new ArgumentNullException(nameof(metadataType));
        }

        MetadataKey = metadataKey;
        MetadataType = metadataType;
    }

    /// <summary>
    /// Gets the metadata key.
    /// </summary>
    public string MetadataKey { get; }

    /// <summary>
    /// Gets the metadata CLR type.
    /// </summary>
    public Type MetadataType { get; }

}

internal static class ValidationAttributeGuard
{
    public static void EnsureNotNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The value must not be null, empty, or whitespace.", parameterName);
        }
    }
}
