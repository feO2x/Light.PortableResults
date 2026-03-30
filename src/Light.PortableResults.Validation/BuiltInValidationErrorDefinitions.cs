using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.Validation;

// See ValidationErrorDefinitionCache.cs for the rationale behind using small immutable value-type keys on the
// built-in definition path.

/// <summary>
/// Exposes reusable built-in validation error definitions.
/// </summary>
public static class BuiltInValidationErrorDefinitions
{
    /// <summary>
    /// Gets the shared definition for null-value validation failures.
    /// </summary>
    public static ValidationErrorDefinition NotNull { get; } = new NotNullValidationErrorDefinition();

    /// <summary>
    /// Gets the shared definition for must-be-null validation failures.
    /// </summary>
    public static ValidationErrorDefinition Null { get; } = new NullValidationErrorDefinition();

    /// <summary>
    /// Gets the shared definition for empty-value validation failures.
    /// </summary>
    public static ValidationErrorDefinition Empty { get; } = new EmptyValidationErrorDefinition();

    /// <summary>
    /// Gets the shared definition for not-empty validation failures.
    /// </summary>
    public static ValidationErrorDefinition NotEmpty { get; } = new NotEmptyValidationErrorDefinition();

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
    /// Gets the shared definition for predicate-based validation failures.
    /// </summary>
    public static ValidationErrorDefinition Predicate { get; } = new PredicateValidationErrorDefinition();

    /// <summary>
    /// Gets or creates a reusable definition for equality validation failures.
    /// </summary>
    public static EqualToValidationErrorDefinition<T> EqualTo<T>(T comparativeValue) =>
        EqualTo(ValidationErrorDefinitionCache.Default, comparativeValue);

    /// <summary>
    /// Gets or creates a reusable definition for equality validation failures.
    /// </summary>
    public static EqualToValidationErrorDefinition<T> EqualTo<T>(
        IValidationErrorDefinitionCache cache,
        T comparativeValue
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<T>(comparativeValue),
            static key => new EqualToValidationErrorDefinition<T>(key.Value)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for inequality validation failures.
    /// </summary>
    public static NotEqualToValidationErrorDefinition<T> NotEqualTo<T>(T comparativeValue) =>
        NotEqualTo(ValidationErrorDefinitionCache.Default, comparativeValue);

    /// <summary>
    /// Gets or creates a reusable definition for inequality validation failures.
    /// </summary>
    public static NotEqualToValidationErrorDefinition<T> NotEqualTo<T>(
        IValidationErrorDefinitionCache cache,
        T comparativeValue
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<T>(comparativeValue),
            static key => new NotEqualToValidationErrorDefinition<T>(key.Value)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for greater-than validation failures.
    /// </summary>
    public static GreaterThanValidationErrorDefinition<T> GreaterThan<T>(T comparativeValue) =>
        GreaterThan(ValidationErrorDefinitionCache.Default, comparativeValue);

    /// <summary>
    /// Gets or creates a reusable definition for greater-than validation failures.
    /// </summary>
    public static GreaterThanValidationErrorDefinition<T> GreaterThan<T>(
        IValidationErrorDefinitionCache cache,
        T comparativeValue
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (comparativeValue is null)
        {
            throw new ArgumentNullException(nameof(comparativeValue));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<T>(comparativeValue),
            static key => new GreaterThanValidationErrorDefinition<T>(key.Value)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for greater-than-or-equal validation failures.
    /// </summary>
    public static GreaterThanOrEqualToValidationErrorDefinition<T> GreaterThanOrEqualTo<T>(T comparativeValue) =>
        GreaterThanOrEqualTo(ValidationErrorDefinitionCache.Default, comparativeValue);

    /// <summary>
    /// Gets or creates a reusable definition for greater-than-or-equal validation failures.
    /// </summary>
    public static GreaterThanOrEqualToValidationErrorDefinition<T> GreaterThanOrEqualTo<T>(
        IValidationErrorDefinitionCache cache,
        T comparativeValue
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (comparativeValue is null)
        {
            throw new ArgumentNullException(nameof(comparativeValue));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<T>(comparativeValue),
            static key => new GreaterThanOrEqualToValidationErrorDefinition<T>(key.Value)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for less-than validation failures.
    /// </summary>
    public static LessThanValidationErrorDefinition<T> LessThan<T>(T comparativeValue) =>
        LessThan(ValidationErrorDefinitionCache.Default, comparativeValue);

    /// <summary>
    /// Gets or creates a reusable definition for less-than validation failures.
    /// </summary>
    public static LessThanValidationErrorDefinition<T> LessThan<T>(
        IValidationErrorDefinitionCache cache,
        T comparativeValue
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (comparativeValue is null)
        {
            throw new ArgumentNullException(nameof(comparativeValue));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<T>(comparativeValue),
            static key => new LessThanValidationErrorDefinition<T>(key.Value)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for less-than-or-equal validation failures.
    /// </summary>
    public static LessThanOrEqualToValidationErrorDefinition<T> LessThanOrEqualTo<T>(T comparativeValue) =>
        LessThanOrEqualTo(ValidationErrorDefinitionCache.Default, comparativeValue);

    /// <summary>
    /// Gets or creates a reusable definition for less-than-or-equal validation failures.
    /// </summary>
    public static LessThanOrEqualToValidationErrorDefinition<T> LessThanOrEqualTo<T>(
        IValidationErrorDefinitionCache cache,
        T comparativeValue
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (comparativeValue is null)
        {
            throw new ArgumentNullException(nameof(comparativeValue));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<T>(comparativeValue),
            static key => new LessThanOrEqualToValidationErrorDefinition<T>(key.Value)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for inclusive-range validation failures.
    /// </summary>
    public static InValidationErrorDefinition<T> IsIn<T>(T lowerBoundary, T upperBoundary) =>
        IsIn(ValidationErrorDefinitionCache.Default, lowerBoundary, upperBoundary);

    /// <summary>
    /// Gets or creates a reusable definition for inclusive-range validation failures.
    /// </summary>
    public static InValidationErrorDefinition<T> IsIn<T>(
        IValidationErrorDefinitionCache cache,
        T lowerBoundary,
        T upperBoundary
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (lowerBoundary is null)
        {
            throw new ArgumentNullException(nameof(lowerBoundary));
        }

        if (upperBoundary is null)
        {
            throw new ArgumentNullException(nameof(upperBoundary));
        }

        return cache.GetOrAdd(
            new RangeDefinitionCacheKey<T>(new ValidationRange<T>(lowerBoundary, upperBoundary)),
            static key => new InValidationErrorDefinition<T>(key.Range.LowerBoundary, key.Range.UpperBoundary)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for outside-range validation failures.
    /// </summary>
    public static NotInValidationErrorDefinition<T> IsNotIn<T>(T lowerBoundary, T upperBoundary) =>
        IsNotIn(ValidationErrorDefinitionCache.Default, lowerBoundary, upperBoundary);

    /// <summary>
    /// Gets or creates a reusable definition for outside-range validation failures.
    /// </summary>
    public static NotInValidationErrorDefinition<T> IsNotIn<T>(
        IValidationErrorDefinitionCache cache,
        T lowerBoundary,
        T upperBoundary
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (lowerBoundary is null)
        {
            throw new ArgumentNullException(nameof(lowerBoundary));
        }

        if (upperBoundary is null)
        {
            throw new ArgumentNullException(nameof(upperBoundary));
        }

        return cache.GetOrAdd(
            new RangeDefinitionCacheKey<T>(new ValidationRange<T>(lowerBoundary, upperBoundary)),
            static key => new NotInValidationErrorDefinition<T>(key.Range.LowerBoundary, key.Range.UpperBoundary)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for exclusive-range validation failures.
    /// </summary>
    public static ExclusiveRangeValidationErrorDefinition<T> IsInExclusiveRange<T>(T lowerBoundary, T upperBoundary) =>
        IsInExclusiveRange(ValidationErrorDefinitionCache.Default, lowerBoundary, upperBoundary);

    /// <summary>
    /// Gets or creates a reusable definition for exclusive-range validation failures.
    /// </summary>
    public static ExclusiveRangeValidationErrorDefinition<T> IsInExclusiveRange<T>(
        IValidationErrorDefinitionCache cache,
        T lowerBoundary,
        T upperBoundary
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (lowerBoundary is null)
        {
            throw new ArgumentNullException(nameof(lowerBoundary));
        }

        if (upperBoundary is null)
        {
            throw new ArgumentNullException(nameof(upperBoundary));
        }

        return cache.GetOrAdd(
            new RangeDefinitionCacheKey<T>(new ValidationRange<T>(lowerBoundary, upperBoundary)),
            static key => new ExclusiveRangeValidationErrorDefinition<T>(
                key.Range.LowerBoundary,
                key.Range.UpperBoundary
            )
        );
    }

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
    /// Gets or creates a reusable definition for exact-count validation failures.
    /// </summary>
    public static CountValidationErrorDefinition Count(int expectedCount) =>
        Count(ValidationErrorDefinitionCache.Default, expectedCount);

    /// <summary>
    /// Gets or creates a reusable definition for exact-count validation failures.
    /// </summary>
    public static CountValidationErrorDefinition Count(
        IValidationErrorDefinitionCache cache,
        int expectedCount
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<int>(expectedCount),
            static key => new CountValidationErrorDefinition(key.Value)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for minimum-count validation failures.
    /// </summary>
    public static MinCountValidationErrorDefinition MinCount(int minCount) =>
        MinCount(ValidationErrorDefinitionCache.Default, minCount);

    /// <summary>
    /// Gets or creates a reusable definition for minimum-count validation failures.
    /// </summary>
    public static MinCountValidationErrorDefinition MinCount(
        IValidationErrorDefinitionCache cache,
        int minCount
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<int>(minCount),
            static key => new MinCountValidationErrorDefinition(key.Value)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for maximum-count validation failures.
    /// </summary>
    public static MaxCountValidationErrorDefinition MaxCount(int maxCount) =>
        MaxCount(ValidationErrorDefinitionCache.Default, maxCount);

    /// <summary>
    /// Gets or creates a reusable definition for maximum-count validation failures.
    /// </summary>
    public static MaxCountValidationErrorDefinition MaxCount(
        IValidationErrorDefinitionCache cache,
        int maxCount
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<int>(maxCount),
            static key => new MaxCountValidationErrorDefinition(key.Value)
        );
    }

    /// <summary>
    /// Gets a reusable definition for enum-value validation failures.
    /// </summary>
    public static EnumValidationErrorDefinition<TEnum> IsInEnum<TEnum>()
        where TEnum : struct, Enum =>
        EnumDefinitionCache<TEnum>.Definition;

    /// <summary>
    /// Gets or creates a reusable definition for enum-name validation failures.
    /// </summary>
    public static EnumNameValidationErrorDefinition<TEnum> EnumName<TEnum>(bool ignoreCase = false)
        where TEnum : struct, Enum =>
        EnumName<TEnum>(ValidationErrorDefinitionCache.Default, ignoreCase);

    /// <summary>
    /// Gets or creates a reusable definition for enum-name validation failures.
    /// </summary>
    public static EnumNameValidationErrorDefinition<TEnum> EnumName<TEnum>(
        IValidationErrorDefinitionCache cache,
        bool ignoreCase = false
    )
        where TEnum : struct, Enum
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<bool>(ignoreCase),
            static key => new EnumNameValidationErrorDefinition<TEnum>(key.Value)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for decimal precision-and-scale validation failures.
    /// </summary>
    public static PrecisionScaleValidationErrorDefinition PrecisionScale(
        int precision,
        int scale,
        bool ignoreTrailingZeros
    ) => PrecisionScale(ValidationErrorDefinitionCache.Default, precision, scale, ignoreTrailingZeros);

    /// <summary>
    /// Gets or creates a reusable definition for decimal precision-and-scale validation failures.
    /// </summary>
    public static PrecisionScaleValidationErrorDefinition PrecisionScale(
        IValidationErrorDefinitionCache cache,
        int precision,
        int scale,
        bool ignoreTrailingZeros
    )
    {
        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return cache.GetOrAdd(
            new SingleValueDefinitionCacheKey<PrecisionScaleDescriptor>(
                new PrecisionScaleDescriptor(precision, scale, ignoreTrailingZeros)
            ),
            static key => new PrecisionScaleValidationErrorDefinition(key.Value)
        );
    }

    private static MetadataObject CreateComparativeMetadata<T>(T comparativeValue) =>
        MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, CreateMetadataValue(comparativeValue)));

    private static MetadataObject CreateRangeMetadata<T>(T lowerBoundary, T upperBoundary) =>
        MetadataObject.Create(
            (ValidationErrorMetadataKeys.LowerBoundary, CreateMetadataValue(lowerBoundary)),
            (ValidationErrorMetadataKeys.UpperBoundary, CreateMetadataValue(upperBoundary))
        );

    private static MetadataObject CreateLengthMetadata(int minLength, int maxLength) =>
        MetadataObject.Create(
            (ValidationErrorMetadataKeys.MinLength, minLength),
            (ValidationErrorMetadataKeys.MaxLength, maxLength)
        );

    private static MetadataObject CreateCountMetadata(string key, int value) =>
        MetadataObject.Create((key, MetadataValue.FromInt64(value)));

    private static MetadataObject CreateRegexMetadata(string pattern, RegexOptions options) =>
        MetadataObject.Create(
            (ValidationErrorMetadataKeys.Pattern, MetadataValue.FromString(pattern)),
            (ValidationErrorMetadataKeys.RegexOptions, MetadataValue.FromInt64((int) options))
        );

    private static MetadataObject CreateEnumMetadata(Type enumType) =>
        MetadataObject.Create(
            (ValidationErrorMetadataKeys.EnumType, MetadataValue.FromString(enumType.FullName ?? enumType.Name))
        );

    private static MetadataObject CreateEnumNameMetadata(Type enumType, bool ignoreCase) =>
        MetadataObject.Create(
            (ValidationErrorMetadataKeys.EnumType, MetadataValue.FromString(enumType.FullName ?? enumType.Name)),
            (ValidationErrorMetadataKeys.IgnoreCase, MetadataValue.FromBoolean(ignoreCase))
        );

    private static MetadataObject CreatePrecisionScaleMetadata(PrecisionScaleDescriptor descriptor) =>
        MetadataObject.Create(
            (ValidationErrorMetadataKeys.ExpectedPrecision, descriptor.Precision),
            (ValidationErrorMetadataKeys.ExpectedScale, descriptor.Scale),
            (ValidationErrorMetadataKeys.IgnoreTrailingZeros, descriptor.IgnoreTrailingZeros)
        );

    private static MetadataValue CreateMetadataValue<T>(T value)
    {
        if (value is null)
        {
            return MetadataValue.Null;
        }

        var type = typeof(T);
        if (type.IsEnum || value is Enum)
        {
            return MetadataValue.FromString(value.ToString());
        }

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
                return MetadataValue.FromBoolean((bool) (object) value);
            case TypeCode.Byte:
                return MetadataValue.FromInt64((byte) (object) value);
            case TypeCode.SByte:
                return MetadataValue.FromInt64((sbyte) (object) value);
            case TypeCode.Int16:
                return MetadataValue.FromInt64((short) (object) value);
            case TypeCode.UInt16:
                return MetadataValue.FromInt64((ushort) (object) value);
            case TypeCode.Int32:
                return MetadataValue.FromInt64((int) (object) value);
            case TypeCode.UInt32:
                return MetadataValue.FromInt64((uint) (object) value);
            case TypeCode.Int64:
                return MetadataValue.FromInt64((long) (object) value);
            case TypeCode.UInt64:
                return MetadataValue.FromString(((ulong) (object) value).ToString(CultureInfo.InvariantCulture));
            case TypeCode.Single:
                return MetadataValue.FromDouble((float) (object) value);
            case TypeCode.Double:
                return MetadataValue.FromDouble((double) (object) value);
            case TypeCode.Decimal:
                return MetadataValue.FromDecimal((decimal) (object) value);
            case TypeCode.Char:
                return MetadataValue.FromString(((char) (object) value).ToString());
            case TypeCode.String:
                return MetadataValue.FromString((string?) (object?) value);
        }

        if (value is MetadataObject metadataObject)
        {
            return MetadataValue.FromObject(metadataObject);
        }

        if (value is MetadataArray metadataArray)
        {
            return MetadataValue.FromArray(metadataArray);
        }

        if (value is IFormattable formattable)
        {
            return MetadataValue.FromString(formattable.ToString(null, CultureInfo.InvariantCulture));
        }

        return MetadataValue.FromString(value.ToString());
    }

    /// <summary>
    /// Reusable built-in validation error definition for null-value validation failures.
    /// </summary>
    public sealed class NotNullValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NotNullValidationErrorDefinition" />.
        /// </summary>
        public NotNullValidationErrorDefinition()
            : base(code: "NotNull") { }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.NotNull.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for must-be-null validation failures.
    /// </summary>
    public sealed class NullValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NullValidationErrorDefinition" />.
        /// </summary>
        public NullValidationErrorDefinition()
            : base(code: "Null") { }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.Null.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for empty-value validation failures.
    /// </summary>
    public sealed class EmptyValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EmptyValidationErrorDefinition" />.
        /// </summary>
        public EmptyValidationErrorDefinition()
            : base(code: "Empty") { }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.Empty.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for not-empty validation failures.
    /// </summary>
    public sealed class NotEmptyValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NotEmptyValidationErrorDefinition" />.
        /// </summary>
        public NotEmptyValidationErrorDefinition()
            : base(code: "NotEmpty") { }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.NotEmpty.ProvideMessage(in context);
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
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.NotNullOrWhiteSpace.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for equality validation failures.
    /// </summary>
    public sealed class EqualToValidationErrorDefinition<T> : ValidationErrorDefinition<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EqualToValidationErrorDefinition{T}" />.
        /// </summary>
        public EqualToValidationErrorDefinition(T comparativeValue)
            : base(
                comparativeValue,
                code: "EqualTo",
                metadata: CreateComparativeMetadata(comparativeValue)
            )
        {
            ComparativeValue = comparativeValue;
        }

        /// <summary>
        /// Gets the expected value.
        /// </summary>
        public T ComparativeValue { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.EqualTo.ProvideMessage(in context, ComparativeValue);
    }

    /// <summary>
    /// Reusable built-in validation error definition for inequality validation failures.
    /// </summary>
    public sealed class NotEqualToValidationErrorDefinition<T> : ValidationErrorDefinition<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NotEqualToValidationErrorDefinition{T}" />.
        /// </summary>
        public NotEqualToValidationErrorDefinition(T comparativeValue)
            : base(
                comparativeValue,
                code: "NotEqualTo",
                metadata: CreateComparativeMetadata(comparativeValue)
            )
        {
            ComparativeValue = comparativeValue;
        }

        /// <summary>
        /// Gets the disallowed value.
        /// </summary>
        public T ComparativeValue { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.NotEqualTo.ProvideMessage(in context, ComparativeValue);
    }

    /// <summary>
    /// Reusable built-in validation error definition for greater-than validation failures.
    /// </summary>
    public sealed class GreaterThanValidationErrorDefinition<T> : ValidationErrorDefinition<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="GreaterThanValidationErrorDefinition{T}" />.
        /// </summary>
        public GreaterThanValidationErrorDefinition(T comparativeValue)
            : base(
                comparativeValue,
                code: "GreaterThan",
                metadata: CreateComparativeMetadata(comparativeValue)
            )
        {
            ComparativeValue = comparativeValue;
        }

        /// <summary>
        /// Gets the boundary value.
        /// </summary>
        public T ComparativeValue { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.GreaterThan.ProvideMessage(in context, ComparativeValue);
    }

    /// <summary>
    /// Reusable built-in validation error definition for greater-than-or-equal validation failures.
    /// </summary>
    public sealed class GreaterThanOrEqualToValidationErrorDefinition<T> : ValidationErrorDefinition<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="GreaterThanOrEqualToValidationErrorDefinition{T}" />.
        /// </summary>
        public GreaterThanOrEqualToValidationErrorDefinition(T comparativeValue)
            : base(
                comparativeValue,
                code: "GreaterThanOrEqualTo",
                metadata: CreateComparativeMetadata(comparativeValue)
            )
        {
            ComparativeValue = comparativeValue;
        }

        /// <summary>
        /// Gets the inclusive lower boundary.
        /// </summary>
        public T ComparativeValue { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.GreaterThanOrEqualTo.ProvideMessage(
                in context,
                ComparativeValue
            );
    }

    /// <summary>
    /// Reusable built-in validation error definition for less-than validation failures.
    /// </summary>
    public sealed class LessThanValidationErrorDefinition<T> : ValidationErrorDefinition<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LessThanValidationErrorDefinition{T}" />.
        /// </summary>
        public LessThanValidationErrorDefinition(T comparativeValue)
            : base(
                comparativeValue,
                code: "LessThan",
                metadata: CreateComparativeMetadata(comparativeValue)
            )
        {
            ComparativeValue = comparativeValue;
        }

        /// <summary>
        /// Gets the boundary value.
        /// </summary>
        public T ComparativeValue { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.LessThan.ProvideMessage(in context, ComparativeValue);
    }

    /// <summary>
    /// Reusable built-in validation error definition for less-than-or-equal validation failures.
    /// </summary>
    public sealed class LessThanOrEqualToValidationErrorDefinition<T> : ValidationErrorDefinition<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LessThanOrEqualToValidationErrorDefinition{T}" />.
        /// </summary>
        public LessThanOrEqualToValidationErrorDefinition(T comparativeValue)
            : base(
                comparativeValue,
                code: "LessThanOrEqualTo",
                metadata: CreateComparativeMetadata(comparativeValue)
            )
        {
            ComparativeValue = comparativeValue;
        }

        /// <summary>
        /// Gets the inclusive upper boundary.
        /// </summary>
        public T ComparativeValue { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.LessThanOrEqualTo.ProvideMessage(
                in context,
                ComparativeValue
            );
    }

    /// <summary>
    /// Reusable built-in validation error definition for inclusive-range validation failures.
    /// </summary>
    public sealed class InValidationErrorDefinition<T> : ValidationErrorDefinition<ValidationRange<T>>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InValidationErrorDefinition{T}" />.
        /// </summary>
        public InValidationErrorDefinition(T lowerBoundary, T upperBoundary)
            : base(
                new ValidationRange<T>(lowerBoundary, upperBoundary),
                code: "IsIn",
                metadata: CreateRangeMetadata(lowerBoundary, upperBoundary)
            )
        {
            LowerBoundary = lowerBoundary;
            UpperBoundary = upperBoundary;
        }

        /// <summary>
        /// Gets the inclusive lower boundary.
        /// </summary>
        public T LowerBoundary { get; }

        /// <summary>
        /// Gets the inclusive upper boundary.
        /// </summary>
        public T UpperBoundary { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.IsIn.ProvideMessage(in context, LowerBoundary, UpperBoundary);
    }

    /// <summary>
    /// Reusable built-in validation error definition for outside-range validation failures.
    /// </summary>
    public sealed class NotInValidationErrorDefinition<T> : ValidationErrorDefinition<ValidationRange<T>>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NotInValidationErrorDefinition{T}" />.
        /// </summary>
        public NotInValidationErrorDefinition(T lowerBoundary, T upperBoundary)
            : base(
                new ValidationRange<T>(lowerBoundary, upperBoundary),
                code: "NotIn",
                metadata: CreateRangeMetadata(lowerBoundary, upperBoundary)
            )
        {
            LowerBoundary = lowerBoundary;
            UpperBoundary = upperBoundary;
        }

        /// <summary>
        /// Gets the lower boundary.
        /// </summary>
        public T LowerBoundary { get; }

        /// <summary>
        /// Gets the upper boundary.
        /// </summary>
        public T UpperBoundary { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.NotIn.ProvideMessage(in context, LowerBoundary, UpperBoundary);
    }

    /// <summary>
    /// Reusable built-in validation error definition for exclusive-range validation failures.
    /// </summary>
    public sealed class ExclusiveRangeValidationErrorDefinition<T> : ValidationErrorDefinition<ValidationRange<T>>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ExclusiveRangeValidationErrorDefinition{T}" />.
        /// </summary>
        public ExclusiveRangeValidationErrorDefinition(T lowerBoundary, T upperBoundary)
            : base(
                new ValidationRange<T>(lowerBoundary, upperBoundary),
                code: "ExclusiveRange",
                metadata: CreateRangeMetadata(lowerBoundary, upperBoundary)
            )
        {
            LowerBoundary = lowerBoundary;
            UpperBoundary = upperBoundary;
        }

        /// <summary>
        /// Gets the exclusive lower boundary.
        /// </summary>
        public T LowerBoundary { get; }

        /// <summary>
        /// Gets the exclusive upper boundary.
        /// </summary>
        public T UpperBoundary { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.ExclusiveRange.ProvideMessage(
                in context,
                LowerBoundary,
                UpperBoundary
            );
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
        public override bool IsMessageStable => true;

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
        public override bool IsMessageStable => true;

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
        public override bool IsMessageStable => true;

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
        public override bool IsMessageStable => true;

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
        public override bool IsMessageStable => true;

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
        public override bool IsMessageStable => true;

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
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.LettersAndDigitsOnly.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for exact-count validation failures.
    /// </summary>
    public sealed class CountValidationErrorDefinition : ValidationErrorDefinition<int>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CountValidationErrorDefinition" />.
        /// </summary>
        public CountValidationErrorDefinition(int expectedCount)
            : base(
                expectedCount,
                code: "Count",
                metadata: CreateCountMetadata(ValidationErrorMetadataKeys.ExpectedCount, expectedCount)
            )
        {
            ExpectedCount = expectedCount;
        }

        /// <summary>
        /// Gets the exact expected count.
        /// </summary>
        public int ExpectedCount { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(
            in ValidationErrorMessageContext<T> context
        ) => context.ValidationContext.ErrorTemplates.Count.ProvideMessage(in context, ExpectedCount);
    }

    /// <summary>
    /// Reusable built-in validation error definition for minimum-count validation failures.
    /// </summary>
    public sealed class MinCountValidationErrorDefinition : ValidationErrorDefinition<int>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MinCountValidationErrorDefinition" />.
        /// </summary>
        public MinCountValidationErrorDefinition(int minCount)
            : base(
                minCount,
                code: "MinCount",
                metadata: CreateCountMetadata(ValidationErrorMetadataKeys.MinCount, minCount)
            )
        {
            MinCount = minCount;
        }

        /// <summary>
        /// Gets the minimum expected count.
        /// </summary>
        public int MinCount { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(
            in ValidationErrorMessageContext<T> context
        ) => context.ValidationContext.ErrorTemplates.MinCount.ProvideMessage(in context, MinCount);
    }

    /// <summary>
    /// Reusable built-in validation error definition for maximum-count validation failures.
    /// </summary>
    public sealed class MaxCountValidationErrorDefinition : ValidationErrorDefinition<int>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MaxCountValidationErrorDefinition" />.
        /// </summary>
        public MaxCountValidationErrorDefinition(int maxCount)
            : base(
                maxCount,
                code: "MaxCount",
                metadata: CreateCountMetadata(ValidationErrorMetadataKeys.MaxCount, maxCount)
            )
        {
            MaxCount = maxCount;
        }

        /// <summary>
        /// Gets the maximum expected count.
        /// </summary>
        public int MaxCount { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(
            in ValidationErrorMessageContext<T> context
        ) => context.ValidationContext.ErrorTemplates.MaxCount.ProvideMessage(in context, MaxCount);
    }

    /// <summary>
    /// Reusable built-in validation error definition for enum-value validation failures.
    /// </summary>
    public sealed class EnumValidationErrorDefinition<TEnum> : ValidationErrorDefinition
        where TEnum : struct, Enum
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EnumValidationErrorDefinition{TEnum}" />.
        /// </summary>
        public EnumValidationErrorDefinition()
            : base(
                code: "Enum",
                metadata: CreateEnumMetadata(typeof(TEnum))
            ) { }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.Enum.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for enum-name validation failures.
    /// </summary>
    public sealed class EnumNameValidationErrorDefinition<TEnum> : ValidationErrorDefinition<bool>
        where TEnum : struct, Enum
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EnumNameValidationErrorDefinition{TEnum}" />.
        /// </summary>
        public EnumNameValidationErrorDefinition(bool ignoreCase)
            : base(
                ignoreCase,
                code: "EnumName",
                metadata: CreateEnumNameMetadata(typeof(TEnum), ignoreCase)
            )
        {
            IgnoreCase = ignoreCase;
        }

        /// <summary>
        /// Gets a value indicating whether enum-name matching ignores case.
        /// </summary>
        public bool IgnoreCase { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.EnumName.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for decimal precision-and-scale validation failures.
    /// </summary>
    public sealed class PrecisionScaleValidationErrorDefinition : ValidationErrorDefinition<PrecisionScaleDescriptor>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PrecisionScaleValidationErrorDefinition" />.
        /// </summary>
        public PrecisionScaleValidationErrorDefinition(PrecisionScaleDescriptor descriptor)
            : base(
                descriptor,
                code: "PrecisionScale",
                metadata: CreatePrecisionScaleMetadata(descriptor)
            )
        {
            Descriptor = descriptor;
        }

        /// <summary>
        /// Gets the precision-and-scale descriptor.
        /// </summary>
        public PrecisionScaleDescriptor Descriptor { get; }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.PrecisionScale.ProvideMessage(in context, Descriptor);
    }

    /// <summary>
    /// Reusable built-in validation error definition for predicate-based validation failures.
    /// </summary>
    public sealed class PredicateValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PredicateValidationErrorDefinition" />.
        /// </summary>
        public PredicateValidationErrorDefinition()
            : base(code: "Predicate") { }

        /// <inheritdoc />
        public override bool IsMessageStable => true;

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.Predicate.ProvideMessage(in context);
    }

    private readonly struct SingleValueDefinitionCacheKey<T> : IEquatable<SingleValueDefinitionCacheKey<T>>
    {
        public SingleValueDefinitionCacheKey(T value) => Value = value;

        public T Value { get; }

        public bool Equals(SingleValueDefinitionCacheKey<T> other) =>
            EqualityComparer<T>.Default.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is SingleValueDefinitionCacheKey<T> other && Equals(other);

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value!);
    }

    private readonly struct RangeDefinitionCacheKey<T> : IEquatable<RangeDefinitionCacheKey<T>>
    {
        public RangeDefinitionCacheKey(ValidationRange<T> range) => Range = range;

        public ValidationRange<T> Range { get; }

        public bool Equals(RangeDefinitionCacheKey<T> other) => Range.Equals(other.Range);

        public override bool Equals(object? obj) => obj is RangeDefinitionCacheKey<T> other && Equals(other);

        public override int GetHashCode() => Range.GetHashCode();
    }

    private readonly struct RegexDefinitionCacheKey : IEquatable<RegexDefinitionCacheKey>
    {
        public RegexDefinitionCacheKey(string pattern, RegexOptions options)
        {
            Pattern = pattern;
            Options = options;
        }

        public string Pattern { get; }
        public RegexOptions Options { get; }

        public bool Equals(RegexDefinitionCacheKey other) =>
            string.Equals(Pattern, other.Pattern, StringComparison.Ordinal) &&
            Options == other.Options;

        public override bool Equals(object? obj) => obj is RegexDefinitionCacheKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Pattern, Options);
    }

    private static class EnumDefinitionCache<TEnum>
        where TEnum : struct, Enum
    {
        public static EnumValidationErrorDefinition<TEnum> Definition { get; } = new ();
    }
}
