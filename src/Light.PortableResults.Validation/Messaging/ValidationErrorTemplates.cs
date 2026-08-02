using System;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation.Messaging;

/// <summary>
/// Provides reusable immutable message templates for built-in validation rules.
/// </summary>
public sealed partial record ValidationErrorTemplates
{
    private static readonly IValidationErrorMessageTemplate DefaultNotNullTemplate =
        new DisplayName(" must not be null");

    private static readonly IValidationErrorMessageTemplate DefaultNullTemplate =
        new DisplayName(" must be null");

    private static readonly IValidationErrorMessageTemplate DefaultEmptyTemplate =
        new DisplayName(" must be empty");

    private static readonly IValidationErrorMessageTemplate DefaultNotEmptyTemplate =
        new DisplayName(" must not be empty");

    private static readonly IValidationErrorMessageTemplate DefaultNotNullOrWhiteSpaceTemplate =
        new DisplayName(" must not be empty or whitespace");

    private static readonly IComparableValidationErrorMessageTemplate DefaultEqualToTemplate =
        new DisplayNameWithComparable(" must be equal to ");

    private static readonly IComparableValidationErrorMessageTemplate DefaultNotEqualToTemplate =
        new DisplayNameWithComparable(" must not be equal to ");

    private static readonly IComparableValidationErrorMessageTemplate DefaultGreaterThanTemplate =
        new DisplayNameWithComparable(" must be greater than ");

    private static readonly IComparableValidationErrorMessageTemplate DefaultGreaterThanOrEqualToTemplate =
        new DisplayNameWithComparable(" must be greater than or equal to ");

    private static readonly IComparableValidationErrorMessageTemplate DefaultLessThanTemplate =
        new DisplayNameWithComparable(" must be less than ");

    private static readonly IComparableValidationErrorMessageTemplate DefaultLessThanOrEqualToTemplate =
        new DisplayNameWithComparable(" must be less than or equal to ");

    private static readonly IRangeValidationErrorMessageTemplate DefaultIsInRangeTemplate =
        new DisplayNameWithRange(" must be between ", " and ");

    private static readonly IRangeValidationErrorMessageTemplate DefaultNotInRangeTemplate =
        new DisplayNameWithRange(" must not be between ", " and ");

    private static readonly IRangeValidationErrorMessageTemplate DefaultExclusiveRangeTemplate =
        new DisplayNameWithRange(" must be between ", " and ", " (exclusive)");

    private static readonly IValidationErrorMessageTemplate<int> DefaultMinLengthTemplate =
        new DisplayNameWithParameter<int>(
            " must be at least ",
            " characters long"
        );

    private static readonly IValidationErrorMessageTemplate<int> DefaultMaxLengthTemplate =
        new DisplayNameWithParameter<int>(
            " must be at most ",
            " characters long"
        );

    private static readonly IRangeValidationErrorMessageTemplate DefaultLengthInTemplate =
        new DisplayNameWithRange(
            " must be between ",
            " and ",
            " characters long"
        );

    private static readonly IValidationErrorMessageTemplate DefaultPatternTemplate =
        new DisplayName(" has an invalid format");

    private static readonly IValidationErrorMessageTemplate DefaultEmailTemplate =
        new DisplayName(" must be an email address");

    private static readonly IValidationErrorMessageTemplate DefaultDigitsOnlyTemplate =
        new DisplayName(" must contain only digits");

    private static readonly IValidationErrorMessageTemplate DefaultLettersAndDigitsOnlyTemplate =
        new DisplayName(" must contain only letters and digits");

    private static readonly IValidationErrorMessageTemplate DefaultUuidV7Template =
        new DisplayName(" must be a version 7 UUID");

    private static readonly IValidationErrorMessageTemplate DefaultUtcTemplate =
        new DisplayName(" must be represented in UTC");

    private static readonly IValidationErrorMessageTemplate DefaultLocalTemplate =
        new DisplayName(" must be a local date and time");

    private static readonly IValidationErrorMessageTemplate DefaultUnspecifiedTemplate =
        new DisplayName(" must not specify a time zone");

    private static readonly IValidationErrorMessageTemplate<int> DefaultCountTemplate =
        new DisplayNameWithParameter<int>(" must contain exactly ", " item(s)");

    private static readonly IValidationErrorMessageTemplate<int> DefaultMinCountTemplate =
        new DisplayNameWithParameter<int>(" must contain at least ", " item(s)");

    private static readonly IValidationErrorMessageTemplate<int> DefaultMaxCountTemplate =
        new DisplayNameWithParameter<int>(" must contain at most ", " item(s)");

    private static readonly IValidationErrorMessageTemplate DefaultEnumTemplate =
        new DisplayName(" must be a defined enum value");

    private static readonly IValidationErrorMessageTemplate DefaultEnumNameTemplate =
        new DisplayName(" must be a valid enum name");

    private static readonly IValidationErrorMessageTemplate<PrecisionScaleDescriptor> DefaultPrecisionScaleTemplate =
        new DisplayNameWithPrecisionScale();

    private static readonly IValidationErrorMessageTemplate DefaultPredicateTemplate =
        new DisplayName(" is invalid");

    private readonly bool _hasExplicitMessageCache;
    private readonly IValidationErrorMessageCache? _messageCache;

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationErrorTemplates" />.
    /// </summary>
    public ValidationErrorTemplates()
    {
        _messageCache = new DefaultValidationErrorMessageCache();
    }

    private ValidationErrorTemplates(ValidationErrorTemplates original)
    {
        NotNull = original.NotNull;
        Null = original.Null;
        Empty = original.Empty;
        NotEmpty = original.NotEmpty;
        NotNullOrWhiteSpace = original.NotNullOrWhiteSpace;
        EqualTo = original.EqualTo;
        NotEqualTo = original.NotEqualTo;
        GreaterThan = original.GreaterThan;
        GreaterThanOrEqualTo = original.GreaterThanOrEqualTo;
        LessThan = original.LessThan;
        LessThanOrEqualTo = original.LessThanOrEqualTo;
        IsInRange = original.IsInRange;
        NotInRange = original.NotInRange;
        ExclusiveRange = original.ExclusiveRange;
        MinLength = original.MinLength;
        MaxLength = original.MaxLength;
        LengthIn = original.LengthIn;
        Pattern = original.Pattern;
        Email = original.Email;
        DigitsOnly = original.DigitsOnly;
        LettersAndDigitsOnly = original.LettersAndDigitsOnly;
        UuidV7 = original.UuidV7;
        Utc = original.Utc;
        Local = original.Local;
        Unspecified = original.Unspecified;
        Count = original.Count;
        MinCount = original.MinCount;
        MaxCount = original.MaxCount;
        Enum = original.Enum;
        EnumName = original.EnumName;
        PrecisionScale = original.PrecisionScale;
        Predicate = original.Predicate;

        if (original._hasExplicitMessageCache)
        {
            _messageCache = original._messageCache;
            _hasExplicitMessageCache = true;
        }
        else
        {
            _messageCache = original._messageCache is null ? null : new DefaultValidationErrorMessageCache();
        }
    }

    private ValidationErrorTemplates(IValidationErrorMessageCache? messageCache)
    {
        _messageCache = messageCache;
    }

    /// <summary>
    /// Gets the shared default templates instance.
    /// </summary>
    public static ValidationErrorTemplates Default { get; } =
        new (DefaultValidationErrorMessageCache.Default);

    /// <summary>
    /// Gets the cache for stable validation error messages. When <see langword="null" />, message caching is disabled.
    /// </summary>
    public IValidationErrorMessageCache? MessageCache
    {
        get => _messageCache;
        init
        {
            _messageCache = value;
            _hasExplicitMessageCache = true;
        }
    }

    /// <summary>
    /// Gets the template for null-value validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate NotNull
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultNotNullTemplate;

    /// <summary>
    /// Gets the template for must-be-null validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate Null
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultNullTemplate;

    /// <summary>
    /// Gets the template for empty-value validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate Empty
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultEmptyTemplate;

    /// <summary>
    /// Gets the template for not-empty validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate NotEmpty
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultNotEmptyTemplate;

    /// <summary>
    /// Gets the template for empty-or-whitespace string validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate NotNullOrWhiteSpace
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultNotNullOrWhiteSpaceTemplate;

    /// <summary>
    /// Gets the template for equality validation failures.
    /// </summary>
    public IComparableValidationErrorMessageTemplate EqualTo
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultEqualToTemplate;

    /// <summary>
    /// Gets the template for inequality validation failures.
    /// </summary>
    public IComparableValidationErrorMessageTemplate NotEqualTo
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultNotEqualToTemplate;

    /// <summary>
    /// Gets the template for greater-than validation failures.
    /// </summary>
    public IComparableValidationErrorMessageTemplate GreaterThan
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultGreaterThanTemplate;

    /// <summary>
    /// Gets the template for greater-than-or-equal validation failures.
    /// </summary>
    public IComparableValidationErrorMessageTemplate GreaterThanOrEqualTo
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultGreaterThanOrEqualToTemplate;

    /// <summary>
    /// Gets the template for less-than validation failures.
    /// </summary>
    public IComparableValidationErrorMessageTemplate LessThan
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultLessThanTemplate;

    /// <summary>
    /// Gets the template for less-than-or-equal validation failures.
    /// </summary>
    public IComparableValidationErrorMessageTemplate LessThanOrEqualTo
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultLessThanOrEqualToTemplate;

    /// <summary>
    /// Gets the template for inclusive-range validation failures.
    /// </summary>
    public IRangeValidationErrorMessageTemplate IsInRange
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultIsInRangeTemplate;

    /// <summary>
    /// Gets the template for outside-range validation failures.
    /// </summary>
    public IRangeValidationErrorMessageTemplate NotInRange
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultNotInRangeTemplate;

    /// <summary>
    /// Gets the template for exclusive-range validation failures.
    /// </summary>
    public IRangeValidationErrorMessageTemplate ExclusiveRange
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultExclusiveRangeTemplate;

    /// <summary>
    /// Gets the template for minimum length validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate<int> MinLength
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultMinLengthTemplate;

    /// <summary>
    /// Gets the template for maximum length validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate<int> MaxLength
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultMaxLengthTemplate;

    /// <summary>
    /// Gets the template for length-range validation failures.
    /// </summary>
    public IRangeValidationErrorMessageTemplate LengthIn
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultLengthInTemplate;

    /// <summary>
    /// Gets the template for regular-expression validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate Pattern
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultPatternTemplate;

    /// <summary>
    /// Gets the template for email validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate Email
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultEmailTemplate;

    /// <summary>
    /// Gets the template for digits-only validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate DigitsOnly
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultDigitsOnlyTemplate;

    /// <summary>
    /// Gets the template for letters-and-digits-only validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate LettersAndDigitsOnly
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultLettersAndDigitsOnlyTemplate;

    /// <summary>
    /// Gets the template for version 7 UUID validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate UuidV7
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultUuidV7Template;

    /// <summary>
    /// Gets the template for UTC date-and-time validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate Utc
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultUtcTemplate;

    /// <summary>
    /// Gets the template for local date-and-time validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate Local
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultLocalTemplate;

    /// <summary>
    /// Gets the template for unspecified-time-zone date-and-time validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate Unspecified
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultUnspecifiedTemplate;

    /// <summary>
    /// Gets the template for exact-count validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate<int> Count
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultCountTemplate;

    /// <summary>
    /// Gets the template for minimum-count validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate<int> MinCount
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultMinCountTemplate;

    /// <summary>
    /// Gets the template for maximum-count validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate<int> MaxCount
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultMaxCountTemplate;

    /// <summary>
    /// Gets the template for enum-value validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate Enum
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultEnumTemplate;

    /// <summary>
    /// Gets the template for enum-name validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate EnumName
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultEnumNameTemplate;

    /// <summary>
    /// Gets the template for decimal precision-and-scale validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate<PrecisionScaleDescriptor> PrecisionScale
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultPrecisionScaleTemplate;

    /// <summary>
    /// Gets the template for predicate-based validation failures.
    /// </summary>
    public IValidationErrorMessageTemplate Predicate
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultPredicateTemplate;
}
