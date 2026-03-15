using System;
using System.Globalization;

namespace Light.PortableResults.Validation;

/// <summary>
/// Represents the immutable configuration for a validation run.
/// </summary>
public sealed record ValidationContextOptions
{
    /// <summary>
    /// Gets the shared default options instance.
    /// </summary>
    public static ValidationContextOptions Default { get; } = new ();

    /// <summary>
    /// Gets the target normalizer that is applied to raw caller argument expressions.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IValidationTargetNormalizer TargetNormalizer
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = ValidationTargets.DefaultNormalizer;

    /// <summary>
    /// Gets the string normalizer applied to string checks.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IStringValueNormalizer StringValueNormalizer
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = TrimStringValueNormalizer.Instance;

    /// <summary>
    /// Gets the culture used for formatting validation message parameters and localization-aware templates.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public CultureInfo CultureInfo
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Gets the provider that decides whether validators automatically create null-validation errors.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IAutomaticNullErrorProvider AutomaticNullErrorProvider
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultAutomaticNullErrorProvider.Instance;

    /// <summary>
    /// Gets the validation error templates used for the validation run.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public ValidationErrorTemplates ErrorTemplates
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = ValidationErrorTemplates.Default;

    /// <summary>
    /// Gets the shared cache for reusable validation error definitions.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IValidationErrorDefinitionCache ErrorDefinitionCache
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = ValidationErrorDefinitionCache.Default;
}
