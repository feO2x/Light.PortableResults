using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

// See ValidationErrorDefinitionCache.cs for the rationale behind using small immutable value-type keys on the
// built-in definition path.

public static partial class BuiltInValidationErrorDefinitions
{
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

    private static bool TryGetStableProvider(IValidationErrorMessageTemplate template, out object provider)
    {
        if (!template.IsMessageStable)
        {
            provider = null!;
            return false;
        }

        provider = template;
        return true;
    }

    private static bool TryGetStableProvider<TParameter>(
        IValidationErrorMessageTemplate<TParameter> template,
        out object provider
    )
    {
        if (!template.IsMessageStable)
        {
            provider = null!;
            return false;
        }

        provider = template;
        return true;
    }

    private static bool TryGetStableProvider(
        IComparableValidationErrorMessageTemplate template,
        out object provider
    )
    {
        if (!template.IsMessageStable)
        {
            provider = null!;
            return false;
        }

        provider = template;
        return true;
    }

    private static bool TryGetStableProvider(
        IRangeValidationErrorMessageTemplate template,
        out object provider
    )
    {
        if (!template.IsMessageStable)
        {
            provider = null!;
            return false;
        }

        provider = template;
        return true;
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
