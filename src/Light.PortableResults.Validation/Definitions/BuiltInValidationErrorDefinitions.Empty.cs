using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

public static partial class BuiltInValidationErrorDefinitions
{
    /// <summary>
    /// Gets the shared definition for empty-value validation failures.
    /// </summary>
    public static ValidationErrorDefinition Empty { get; } = new EmptyValidationErrorDefinition();

    /// <summary>
    /// Gets the shared definition for not-empty validation failures.
    /// </summary>
    public static ValidationErrorDefinition NotEmpty { get; } = new NotEmptyValidationErrorDefinition();

    /// <summary>
    /// Reusable built-in validation error definition for empty-value validation failures.
    /// </summary>
    public sealed class EmptyValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EmptyValidationErrorDefinition" />.
        /// </summary>
        public EmptyValidationErrorDefinition()
            : base(code: ValidationErrorCodes.Empty) { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.Empty, out provider);

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
            : base(code: ValidationErrorCodes.NotEmpty) { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.NotEmpty, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.NotEmpty.ProvideMessage(in context);
    }
}
