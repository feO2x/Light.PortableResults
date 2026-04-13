using System;
using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

public static partial class BuiltInValidationErrorDefinitions
{
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
    public static InBetweenValidationErrorDefinition<T> IsInBetween<T>(T lowerBoundary, T upperBoundary) =>
        IsInBetween(ValidationErrorDefinitionCache.Default, lowerBoundary, upperBoundary);

    /// <summary>
    /// Gets or creates a reusable definition for inclusive-range validation failures.
    /// </summary>
    public static InBetweenValidationErrorDefinition<T> IsInBetween<T>(
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
            static key => new InBetweenValidationErrorDefinition<T>(key.Range.LowerBoundary, key.Range.UpperBoundary)
        );
    }

    /// <summary>
    /// Gets or creates a reusable definition for outside-range validation failures.
    /// </summary>
    public static NotInBetweenValidationErrorDefinition<T> IsNotInBetween<T>(T lowerBoundary, T upperBoundary) =>
        IsNotInBetween(ValidationErrorDefinitionCache.Default, lowerBoundary, upperBoundary);

    /// <summary>
    /// Gets or creates a reusable definition for outside-range validation failures.
    /// </summary>
    public static NotInBetweenValidationErrorDefinition<T> IsNotInBetween<T>(
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
            static key => new NotInBetweenValidationErrorDefinition<T>(key.Range.LowerBoundary, key.Range.UpperBoundary)
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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.GreaterThan, out provider);

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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.GreaterThanOrEqualTo, out provider);

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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.LessThan, out provider);

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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.LessThanOrEqualTo, out provider);

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
    public sealed class InBetweenValidationErrorDefinition<T> : ValidationErrorDefinition<ValidationRange<T>>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InBetweenValidationErrorDefinition{T}" />.
        /// </summary>
        public InBetweenValidationErrorDefinition(T lowerBoundary, T upperBoundary)
            : base(
                new ValidationRange<T>(lowerBoundary, upperBoundary),
                code: "IsInBetween",
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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.IsInBetween, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.IsInBetween.ProvideMessage(in context, LowerBoundary, UpperBoundary);
    }

    /// <summary>
    /// Reusable built-in validation error definition for outside-range validation failures.
    /// </summary>
    public sealed class NotInBetweenValidationErrorDefinition<T> : ValidationErrorDefinition<ValidationRange<T>>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NotInBetweenValidationErrorDefinition{T}" />.
        /// </summary>
        public NotInBetweenValidationErrorDefinition(T lowerBoundary, T upperBoundary)
            : base(
                new ValidationRange<T>(lowerBoundary, upperBoundary),
                code: "NotInBetween",
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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.NotInBetween, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.NotInBetween.ProvideMessage(in context, LowerBoundary, UpperBoundary);
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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.ExclusiveRange, out provider);

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
}
