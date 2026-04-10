using Light.PortableResults.Metadata;

namespace Light.PortableResults.Validation;

/// <summary>
/// Represents lightweight inline overrides for built-in validation assertion failures.
/// Use <see cref="Definitions.ValidationErrorDefinition" /> when the rule identity should be reusable across call
/// sites, and use <see cref="Checks.Custom{T}(Check{T}, System.Action{ValidationContext, T})" /> together with
/// <see cref="ValidationContext.AddError(string, string?, Targeting.ValidationTarget?, MetadataObject?)" /> for
/// imperative validation logic that does not map to a single built-in assertion.
/// </summary>
/// <example>
/// <code>
/// context.Check(dto.Comment).IsNotNullOrWhiteSpace("Comment must be present");
/// </code>
/// </example>
/// <example>
/// <code>
/// context.Check(dto.Comment).IsNotNullOrWhiteSpace(
///     new ValidationErrorOverrides
///     {
///         Message = "Comment must be present",
///         Code = "CommentRequired",
///         Category = ErrorCategory.UnprocessableContent
///     }
/// );
/// </code>
/// </example>
public readonly record struct ValidationErrorOverrides
{
    /// <summary>
    /// Gets the override for the final human-readable error message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the override for the final error code.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets the override for the final error category.
    /// </summary>
    public ErrorCategory? Category { get; init; }

    /// <summary>
    /// Gets the override for the final error metadata.
    /// </summary>
    public MetadataObject? Metadata { get; init; }

    /// <summary>
    /// Creates overrides for the common message-only case.
    /// </summary>
    /// <param name="message">The replacement error message.</param>
    public static implicit operator ValidationErrorOverrides(string message) => new () { Message = message };
}
