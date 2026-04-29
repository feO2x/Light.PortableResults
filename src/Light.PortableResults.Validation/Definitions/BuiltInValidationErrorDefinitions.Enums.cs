using System;
using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

public static partial class BuiltInValidationErrorDefinitions
{
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
                code: ValidationErrorCodes.Enum,
                metadata: CreateEnumMetadata(typeof(TEnum))
            ) { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.Enum, out provider);

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
                code: ValidationErrorCodes.EnumName,
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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.EnumName, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.EnumName.ProvideMessage(in context);
    }
}
