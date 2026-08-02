using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

public static partial class BuiltInValidationErrorDefinitions
{
    /// <summary>
    /// Gets the shared definition for version 7 UUID validation failures.
    /// </summary>
    public static ValidationErrorDefinition UuidV7 { get; } = new UuidV7ValidationErrorDefinition();

    /// <summary>
    /// Reusable built-in validation error definition for version 7 UUID validation failures.
    /// </summary>
    public sealed class UuidV7ValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UuidV7ValidationErrorDefinition" />.
        /// </summary>
        public UuidV7ValidationErrorDefinition() : base(code: ValidationErrorCodes.UuidV7) { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.UuidV7, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.UuidV7.ProvideMessage(in context);
    }
}
