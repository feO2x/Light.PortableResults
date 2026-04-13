using System;
using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

public static partial class BuiltInValidationErrorDefinitions
{
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
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.PrecisionScale, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.PrecisionScale.ProvideMessage(in context, Descriptor);
    }
}
