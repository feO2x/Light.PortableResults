using System;
using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

public static partial class BuiltInValidationErrorDefinitions
{
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
                code: ValidationErrorCodes.EqualTo,
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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.EqualTo, out provider);

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
                code: ValidationErrorCodes.NotEqualTo,
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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.NotEqualTo, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<TValue>(
            in ValidationErrorMessageContext<TValue> context
        ) =>
            context.ValidationContext.ErrorTemplates.NotEqualTo.ProvideMessage(in context, ComparativeValue);
    }
}
