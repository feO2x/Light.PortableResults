using System;
using System.Collections.Generic;
using System.Globalization;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.Validation;

// We intentionally model the built-in cache keys in this file as small immutable value types instead of classes.
// These keys represent transient rule-identity values for hot lookup paths such as GreaterThan(18) or IsIn(1, 10),
// so a class-based key would usually allocate per cache lookup unless callers also cached the key instance.
// That extra allocation would undercut the point of reusing immutable validation error definitions in the first place.
//
// This is different from ValidationContextKey<T> in ValidationState. Those keys typically identify long-lived shared
// state slots, are often created once and reused, and therefore tolerate reference-type identity objects better.
// The definition-cache keys here are short-lived value carriers whose semantics are fully described by one or two
// typed boundary values, which makes readonly structs a better fit.
//
// The public cache API still remains generic, so advanced callers can use their own key types, whether class- or
// struct-based. We keep the built-in path allocation-free by default and avoid introducing a public inheritance-based
// key hierarchy that would add ceremony without improving the equality semantics of these concrete rule identities.

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
    /// Gets or creates a reusable definition for greater-than validation failures.
    /// </summary>
    /// <typeparam name="T">The comparison type.</typeparam>
    /// <param name="comparativeValue">The boundary value.</param>
    /// <returns>The reusable definition.</returns>
    public static GreaterThanValidationErrorDefinition<T> GreaterThan<T>(T comparativeValue) =>
        GreaterThan(ValidationErrorDefinitionCache.Default, comparativeValue);

    /// <summary>
    /// Gets or creates a reusable definition for greater-than validation failures.
    /// </summary>
    /// <typeparam name="T">The comparison type.</typeparam>
    /// <param name="cache">The shared definition cache.</param>
    /// <param name="comparativeValue">The boundary value.</param>
    /// <returns>The reusable definition.</returns>
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
            new GreaterThanDefinitionCacheKey<T>(comparativeValue),
            static key => new GreaterThanValidationErrorDefinition<T>(key.ComparativeValue)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for less-than validation failures.
    /// </summary>
    /// <typeparam name="T">The comparison type.</typeparam>
    /// <param name="comparativeValue">The boundary value.</param>
    /// <returns>The reusable definition.</returns>
    public static LessThanValidationErrorDefinition<T> LessThan<T>(T comparativeValue) =>
        LessThan(ValidationErrorDefinitionCache.Default, comparativeValue);

    /// <summary>
    /// Gets or creates a reusable definition for less-than validation failures.
    /// </summary>
    /// <typeparam name="T">The comparison type.</typeparam>
    /// <param name="cache">The shared definition cache.</param>
    /// <param name="comparativeValue">The boundary value.</param>
    /// <returns>The reusable definition.</returns>
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
            new LessThanDefinitionCacheKey<T>(comparativeValue),
            static key => new LessThanValidationErrorDefinition<T>(key.ComparativeValue)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for inclusive range validation failures.
    /// </summary>
    /// <typeparam name="T">The boundary type.</typeparam>
    /// <param name="lowerBoundary">The inclusive lower boundary.</param>
    /// <param name="upperBoundary">The inclusive upper boundary.</param>
    /// <returns>The reusable definition.</returns>
    public static InValidationErrorDefinition<T> IsIn<T>(T lowerBoundary, T upperBoundary) =>
        IsIn(ValidationErrorDefinitionCache.Default, lowerBoundary, upperBoundary);

    /// <summary>
    /// Gets or creates a reusable definition for inclusive range validation failures.
    /// </summary>
    /// <typeparam name="T">The boundary type.</typeparam>
    /// <param name="cache">The shared definition cache.</param>
    /// <param name="lowerBoundary">The inclusive lower boundary.</param>
    /// <param name="upperBoundary">The inclusive upper boundary.</param>
    /// <returns>The reusable definition.</returns>
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
            new InDefinitionCacheKey<T>(new ValidationRange<T>(lowerBoundary, upperBoundary)),
            static key => new InValidationErrorDefinition<T>(key.Range.LowerBoundary, key.Range.UpperBoundary)
        );
    }

    private static MetadataObject CreateComparativeMetadata<T>(T comparativeValue) =>
        MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, CreateMetadataValue(comparativeValue)));

    private static MetadataObject CreateRangeMetadata<T>(T lowerBoundary, T upperBoundary) =>
        MetadataObject.Create(
            (ValidationErrorMetadataKeys.LowerBoundary, CreateMetadataValue(lowerBoundary)),
            (ValidationErrorMetadataKeys.UpperBoundary, CreateMetadataValue(upperBoundary))
        );

    private static MetadataValue CreateMetadataValue<T>(T value)
    {
        if (value is null)
        {
            return MetadataValue.Null;
        }

        switch (Type.GetTypeCode(typeof(T)))
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
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.NotNull.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for greater-than validation failures.
    /// </summary>
    /// <typeparam name="T">The boundary type.</typeparam>
    public sealed class GreaterThanValidationErrorDefinition<T> : ValidationErrorDefinition<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="GreaterThanValidationErrorDefinition{T}" />.
        /// </summary>
        /// <param name="comparativeValue">The boundary value.</param>
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
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.GreaterThan.ProvideMessage(in context, ComparativeValue);
    }

    /// <summary>
    /// Reusable built-in validation error definition for less-than validation failures.
    /// </summary>
    /// <typeparam name="T">The boundary type.</typeparam>
    public sealed class LessThanValidationErrorDefinition<T> : ValidationErrorDefinition<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LessThanValidationErrorDefinition{T}" />.
        /// </summary>
        /// <param name="comparativeValue">The boundary value.</param>
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
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.LessThan.ProvideMessage(in context, ComparativeValue);
    }

    /// <summary>
    /// Reusable built-in validation error definition for inclusive range validation failures.
    /// </summary>
    /// <typeparam name="T">The boundary type.</typeparam>
    public sealed class InValidationErrorDefinition<T> : ValidationErrorDefinition<ValidationRange<T>>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InValidationErrorDefinition{T}" />.
        /// </summary>
        /// <param name="lowerBoundary">The inclusive lower boundary.</param>
        /// <param name="upperBoundary">The inclusive upper boundary.</param>
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
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.IsIn.ProvideMessage(in context, LowerBoundary, UpperBoundary);
    }

    private readonly struct GreaterThanDefinitionCacheKey<T> : IEquatable<GreaterThanDefinitionCacheKey<T>>
    {
        public GreaterThanDefinitionCacheKey(T comparativeValue) => ComparativeValue = comparativeValue;

        public T ComparativeValue { get; }

        public bool Equals(GreaterThanDefinitionCacheKey<T> other) =>
            EqualityComparer<T>.Default.Equals(ComparativeValue, other.ComparativeValue);

        public override bool Equals(object? obj) =>
            obj is GreaterThanDefinitionCacheKey<T> other && Equals(other);

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(ComparativeValue!);
    }

    private readonly struct LessThanDefinitionCacheKey<T> : IEquatable<LessThanDefinitionCacheKey<T>>
    {
        public LessThanDefinitionCacheKey(T comparativeValue) => ComparativeValue = comparativeValue;

        public T ComparativeValue { get; }

        public bool Equals(LessThanDefinitionCacheKey<T> other) =>
            EqualityComparer<T>.Default.Equals(ComparativeValue, other.ComparativeValue);

        public override bool Equals(object? obj) =>
            obj is LessThanDefinitionCacheKey<T> other && Equals(other);

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(ComparativeValue!);
    }

    private readonly struct InDefinitionCacheKey<T> : IEquatable<InDefinitionCacheKey<T>>
    {
        public InDefinitionCacheKey(ValidationRange<T> range) => Range = range;

        public ValidationRange<T> Range { get; }

        public bool Equals(InDefinitionCacheKey<T> other) => Range.Equals(other.Range);

        public override bool Equals(object? obj) => obj is InDefinitionCacheKey<T> other && Equals(other);

        public override int GetHashCode() => Range.GetHashCode();
    }
}
