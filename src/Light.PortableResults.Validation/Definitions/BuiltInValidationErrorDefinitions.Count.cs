using System;
using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

public static partial class BuiltInValidationErrorDefinitions
{
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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.Count, out provider);

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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.MinCount, out provider);

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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.MaxCount, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(
            in ValidationErrorMessageContext<T> context
        ) => context.ValidationContext.ErrorTemplates.MaxCount.ProvideMessage(in context, MaxCount);
    }
}
