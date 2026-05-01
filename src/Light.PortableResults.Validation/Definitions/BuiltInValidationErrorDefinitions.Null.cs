using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

public static partial class BuiltInValidationErrorDefinitions
{
    /// <summary>
    /// Gets the shared definition for null-value validation failures.
    /// </summary>
    public static ValidationErrorDefinition NotNull { get; } = new NotNullValidationErrorDefinition();

    /// <summary>
    /// Gets the shared definition for must-be-null validation failures.
    /// </summary>
    public static ValidationErrorDefinition Null { get; } = new NullValidationErrorDefinition();

    /// <summary>
    /// Reusable built-in validation error definition for null-value validation failures.
    /// </summary>
    public sealed class NotNullValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NotNullValidationErrorDefinition" />.
        /// </summary>
        public NotNullValidationErrorDefinition()
            : base(code: ValidationErrorCodes.NotNull) { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.NotNull, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.NotNull.ProvideMessage(in context);
    }

    /// <summary>
    /// Reusable built-in validation error definition for must-be-null validation failures.
    /// </summary>
    public sealed class NullValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NullValidationErrorDefinition" />.
        /// </summary>
        public NullValidationErrorDefinition()
            : base(code: ValidationErrorCodes.Null) { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.Null, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.Null.ProvideMessage(in context);
    }
}
