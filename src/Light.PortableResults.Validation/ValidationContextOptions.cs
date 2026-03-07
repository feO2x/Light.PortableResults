using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Represents options for validation context creation and behavior.
/// </summary>
public class ValidationContextOptions
{
    private IValidationTargetNormalizer _targetNormalizer = ValidationTargets.DefaultNormalizer;

    /// <summary>
    /// Gets the shared default options instance.
    /// </summary>
    public static ValidationContextOptions Default { get; } = new ();

    /// <summary>
    /// Gets or sets the target normalizer that is applied to raw caller argument expressions.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IValidationTargetNormalizer TargetNormalizer
    {
        get => _targetNormalizer;
        set => _targetNormalizer = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets a value indicating whether string values are normalized when checks are created.
    /// </summary>
    public bool NormalizeStringValues { get; set; } = true;

    /// <summary>
    /// Gets or sets the string normalization function.
    /// </summary>
    public Func<string?, string> NormalizeStringValue { get; set; } = static value => value?.Trim() ?? string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether validators automatically create a validation error for null source values.
    /// </summary>
    public bool CreateAutomaticNullErrors { get; set; } = true;

    /// <summary>
    /// Gets or sets a custom factory for the automatic null-validation error.
    /// </summary>
    public Func<ValidationContext, string, string, Error>? CreateAutomaticNullError { get; set; }
}
