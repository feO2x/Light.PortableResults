using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

public static partial class BuiltInValidationErrorDefinitions
{
    /// <summary>
    /// Gets the shared definition for UTC date-and-time validation failures.
    /// </summary>
    public static ValidationErrorDefinition Utc { get; } = new UtcValidationErrorDefinition();

    /// <summary>
    /// Gets the shared definition for local date-and-time validation failures.
    /// </summary>
    public static ValidationErrorDefinition Local { get; } = new LocalValidationErrorDefinition();

    /// <summary>
    /// Gets the shared definition for unspecified-time-zone date-and-time validation failures.
    /// </summary>
    public static ValidationErrorDefinition Unspecified { get; } = new UnspecifiedValidationErrorDefinition();

    /// <summary>
    /// Reusable built-in validation error definition for UTC date-and-time validation failures.
    /// </summary>
    public sealed class UtcValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UtcValidationErrorDefinition" />.
        /// </summary>
        public UtcValidationErrorDefinition() : base(code: ValidationErrorCodes.Utc) { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.Utc, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.Utc.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for local date-and-time validation failures.
    /// </summary>
    public sealed class LocalValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LocalValidationErrorDefinition" />.
        /// </summary>
        public LocalValidationErrorDefinition() : base(code: ValidationErrorCodes.Local) { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.Local, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.Local.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for unspecified-time-zone date-and-time validation failures.
    /// </summary>
    public sealed class UnspecifiedValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnspecifiedValidationErrorDefinition" />.
        /// </summary>
        public UnspecifiedValidationErrorDefinition() : base(code: ValidationErrorCodes.Unspecified) { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.Unspecified, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.Unspecified.ProvideMessage(in context);
    }
}
