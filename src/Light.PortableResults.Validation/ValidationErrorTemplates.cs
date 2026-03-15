using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides reusable immutable message templates for validation errors.
/// </summary>
public sealed record ValidationErrorTemplates
{
    private static readonly IValidationErrorMessageTemplate DefaultNotNullTemplate =
        new DisplayNameValidationErrorMessageTemplate(" must not be null");

    private static readonly IValidationErrorMessageTemplate DefaultNotNullOrWhiteSpaceTemplate =
        new DisplayNameValidationErrorMessageTemplate(" must not be empty");

    private static readonly IValidationErrorMessageTemplate<int> DefaultMinLengthTemplate =
        new DisplayNameWithParameterValidationErrorMessageTemplate<int>(
            " must be at least ",
            " characters long"
        );

    private static readonly IValidationErrorMessageTemplate<int> DefaultMaxLengthTemplate =
        new DisplayNameWithParameterValidationErrorMessageTemplate<int>(
            " must be at most ",
            " characters long"
        );

    private static readonly IValidationErrorMessageTemplate DefaultPatternTemplate =
        new DisplayNameValidationErrorMessageTemplate(" has an invalid format");

    private static readonly IValidationErrorMessageTemplate DefaultEmailTemplate =
        new DisplayNameValidationErrorMessageTemplate(" must be an email address");

    private static readonly IComparableValidationErrorMessageTemplate DefaultGreaterThanTemplate =
        new DisplayNameWithComparableValidationErrorMessageTemplate(" must be greater than ");

    private static readonly IComparableValidationErrorMessageTemplate DefaultLessThanTemplate =
        new DisplayNameWithComparableValidationErrorMessageTemplate(" must be less than ");

    private static readonly IRangeValidationErrorMessageTemplate DefaultIsInTemplate =
        new DisplayNameWithRangeValidationErrorMessageTemplate(" must be between ", " and ");

    /// <summary>
    /// Gets the shared default templates instance.
    /// </summary>
    public static ValidationErrorTemplates Default { get; } = new ();

    /// <summary>
    /// Gets the template for null-value validation failures.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IValidationErrorMessageTemplate NotNull
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultNotNullTemplate;

    /// <summary>
    /// Gets the template for empty or whitespace string validation failures.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IValidationErrorMessageTemplate NotNullOrWhiteSpace
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultNotNullOrWhiteSpaceTemplate;

    /// <summary>
    /// Gets the template for minimum length validation failures.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IValidationErrorMessageTemplate<int> MinLength
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultMinLengthTemplate;

    /// <summary>
    /// Gets the template for maximum length validation failures.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IValidationErrorMessageTemplate<int> MaxLength
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultMaxLengthTemplate;

    /// <summary>
    /// Gets the template for invalid pattern validation failures.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IValidationErrorMessageTemplate Pattern
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultPatternTemplate;

    /// <summary>
    /// Gets the template for email validation failures.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IValidationErrorMessageTemplate Email
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultEmailTemplate;

    /// <summary>
    /// Gets the template for greater-than validation failures.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IComparableValidationErrorMessageTemplate GreaterThan
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultGreaterThanTemplate;

    /// <summary>
    /// Gets the template for less-than validation failures.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IComparableValidationErrorMessageTemplate LessThan
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultLessThanTemplate;

    /// <summary>
    /// Gets the template for inclusive range validation failures.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IRangeValidationErrorMessageTemplate IsIn
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultIsInTemplate;
}
