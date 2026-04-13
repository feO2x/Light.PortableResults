using System;
using System.Text.RegularExpressions;
using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

public static partial class BuiltInValidationErrorDefinitions
{
    /// <summary>
    /// Gets the shared definition for null-or-empty-or-whitespace string validation failures.
    /// </summary>
    public static ValidationErrorDefinition NotNullOrWhiteSpace { get; } =
        new NotNullOrWhiteSpaceValidationErrorDefinition();

    /// <summary>
    /// Gets the shared definition for email validation failures.
    /// </summary>
    public static ValidationErrorDefinition Email { get; } = new EmailValidationErrorDefinition();

    /// <summary>
    /// Gets the shared definition for digits-only validation failures.
    /// </summary>
    public static ValidationErrorDefinition DigitsOnly { get; } = new DigitsOnlyValidationErrorDefinition();

    /// <summary>
    /// Gets the shared definition for letters-and-digits-only validation failures.
    /// </summary>
    public static ValidationErrorDefinition LettersAndDigitsOnly { get; } =
        new LettersAndDigitsOnlyValidationErrorDefinition();

    /// <summary>
    /// Gets or creates a reusable definition for minimum-length validation failures.
    /// </summary>
    public static MinLengthValidationErrorDefinition MinLength(int minLength) =>
        MinLength(ValidationErrorDefinitionCache.Default, minLength);

    /// <summary>
    /// Gets or creates a reusable definition for minimum-length validation failures.
    /// </summary>
    public static MinLengthValidationErrorDefinition MinLength(
        IValidationErrorDefinitionCache cache,
        int minLength
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<int>(minLength),
            static key => new MinLengthValidationErrorDefinition(key.Value)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for maximum-length validation failures.
    /// </summary>
    public static MaxLengthValidationErrorDefinition MaxLength(int maxLength) =>
        MaxLength(ValidationErrorDefinitionCache.Default, maxLength);

    /// <summary>
    /// Gets or creates a reusable definition for maximum-length validation failures.
    /// </summary>
    public static MaxLengthValidationErrorDefinition MaxLength(
        IValidationErrorDefinitionCache cache,
        int maxLength
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<int>(maxLength),
            static key => new MaxLengthValidationErrorDefinition(key.Value)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for length-range validation failures.
    /// </summary>
    public static LengthInValidationErrorDefinition LengthIn(int minLength, int maxLength) =>
        LengthIn(ValidationErrorDefinitionCache.Default, minLength, maxLength);

    /// <summary>
    /// Gets or creates a reusable definition for length-range validation failures.
    /// </summary>
    public static LengthInValidationErrorDefinition LengthIn(
        IValidationErrorDefinitionCache cache,
        int minLength,
        int maxLength
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return cache.GetOrAdd(
            new RangeDefinitionCacheKey<int>(new ValidationRange<int>(minLength, maxLength)),
            static key => new LengthInValidationErrorDefinition(key.Range.LowerBoundary, key.Range.UpperBoundary)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for regular-expression validation failures.
    /// </summary>
    public static PatternValidationErrorDefinition Matches(string pattern, RegexOptions options = RegexOptions.None) =>
        Matches(ValidationErrorDefinitionCache.Default, pattern, options);

    /// <summary>
    /// Gets or creates a reusable definition for regular-expression validation failures.
    /// </summary>
    public static PatternValidationErrorDefinition Matches(
        IValidationErrorDefinitionCache cache,
        string pattern,
        RegexOptions options = RegexOptions.None
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        return cache.GetOrAdd(
            new RegexDefinitionCacheKey(pattern, options),
            static key => new PatternValidationErrorDefinition(key.Pattern, key.Options)
        );
    }

    /// <summary>
    /// Reusable built-in validation error definition for null-or-empty-or-whitespace string validation failures.
    /// </summary>
    public sealed class NotNullOrWhiteSpaceValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NotNullOrWhiteSpaceValidationErrorDefinition" />.
        /// </summary>
        public NotNullOrWhiteSpaceValidationErrorDefinition()
            : base(code: "NotNullOrWhiteSpace") { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.NotNullOrWhiteSpace, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.NotNullOrWhiteSpace.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for minimum-length validation failures.
    /// </summary>
    public sealed class MinLengthValidationErrorDefinition : ValidationErrorDefinition<int>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MinLengthValidationErrorDefinition" />.
        /// </summary>
        public MinLengthValidationErrorDefinition(int minLength)
            : base(
                minLength,
                code: "MinLength",
                metadata: CreateCountMetadata(ValidationErrorMetadataKeys.MinLength, minLength)
            )
        {
            MinLength = minLength;
        }

        /// <summary>
        /// Gets the minimum allowed length.
        /// </summary>
        public int MinLength { get; }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.MinLength, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(
            in ValidationErrorMessageContext<T> context
        ) => context.ValidationContext.ErrorTemplates.MinLength.ProvideMessage(in context, MinLength);
    }

    /// <summary>
    /// Reusable built-in validation error definition for maximum-length validation failures.
    /// </summary>
    public sealed class MaxLengthValidationErrorDefinition : ValidationErrorDefinition<int>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MaxLengthValidationErrorDefinition" />.
        /// </summary>
        public MaxLengthValidationErrorDefinition(int maxLength)
            : base(
                maxLength,
                code: "MaxLength",
                metadata: CreateCountMetadata(ValidationErrorMetadataKeys.MaxLength, maxLength)
            )
        {
            MaxLength = maxLength;
        }

        /// <summary>
        /// Gets the maximum allowed length.
        /// </summary>
        public int MaxLength { get; }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.MaxLength, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(
            in ValidationErrorMessageContext<T> context
        ) => context.ValidationContext.ErrorTemplates.MaxLength.ProvideMessage(in context, MaxLength);
    }

    /// <summary>
    /// Reusable built-in validation error definition for length-range validation failures.
    /// </summary>
    public sealed class LengthInValidationErrorDefinition : ValidationErrorDefinition<ValidationRange<int>>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LengthInValidationErrorDefinition" />.
        /// </summary>
        public LengthInValidationErrorDefinition(int minLength, int maxLength)
            : base(
                new ValidationRange<int>(minLength, maxLength),
                code: "LengthIn",
                metadata: CreateLengthMetadata(minLength, maxLength)
            )
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }

        /// <summary>
        /// Gets the minimum allowed length.
        /// </summary>
        public int MinLength { get; }

        /// <summary>
        /// Gets the maximum allowed length.
        /// </summary>
        public int MaxLength { get; }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.LengthIn, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(
            in ValidationErrorMessageContext<T> context
        ) => context.ValidationContext.ErrorTemplates.LengthIn.ProvideMessage(in context, MinLength, MaxLength);
    }

    /// <summary>
    /// Reusable built-in validation error definition for regular-expression validation failures.
    /// </summary>
    public sealed class PatternValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PatternValidationErrorDefinition" />.
        /// </summary>
        public PatternValidationErrorDefinition(string pattern, RegexOptions options)
            : base(
                code: "Matches",
                metadata: CreateRegexMetadata(pattern, options)
            )
        {
            Pattern = pattern;
            Options = options;
            Regex = new Regex(pattern, options);
        }

        /// <summary>
        /// Gets the regular-expression pattern.
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Gets the regular-expression options.
        /// </summary>
        public RegexOptions Options { get; }

        /// <summary>
        /// Gets the cached regular expression instance.
        /// </summary>
        public Regex Regex { get; }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.Pattern, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.Pattern.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for email validation failures.
    /// </summary>
    public sealed class EmailValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EmailValidationErrorDefinition" />.
        /// </summary>
        public EmailValidationErrorDefinition()
            : base(code: "Email") { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.Email, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.Email.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for digits-only validation failures.
    /// </summary>
    public sealed class DigitsOnlyValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DigitsOnlyValidationErrorDefinition" />.
        /// </summary>
        public DigitsOnlyValidationErrorDefinition()
            : base(code: "DigitsOnly") { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.DigitsOnly, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.DigitsOnly.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for letters-and-digits-only validation failures.
    /// </summary>
    public sealed class LettersAndDigitsOnlyValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LettersAndDigitsOnlyValidationErrorDefinition" />.
        /// </summary>
        public LettersAndDigitsOnlyValidationErrorDefinition()
            : base(code: "LettersAndDigitsOnly") { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.LettersAndDigitsOnly, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.LettersAndDigitsOnly.ProvideMessage(in context);
    }
}
