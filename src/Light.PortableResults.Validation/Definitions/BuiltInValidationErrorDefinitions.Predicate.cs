using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation.Definitions;

public static partial class BuiltInValidationErrorDefinitions
{
    /// <summary>
    /// Gets the shared definition for predicate-based validation failures.
    /// </summary>
    public static ValidationErrorDefinition Predicate { get; } = new PredicateValidationErrorDefinition();

    /// <summary>
    /// Reusable built-in validation error definition for predicate-based validation failures.
    /// </summary>
    public sealed class PredicateValidationErrorDefinition : ValidationErrorDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PredicateValidationErrorDefinition" />.
        /// </summary>
        public PredicateValidationErrorDefinition()
            : base(code: ValidationErrorCodes.Predicate) { }

        /// <inheritdoc />
        public override bool TryGetStableMessageProvider(
            ReadOnlyValidationContext context,
            out object provider
        ) => TryGetStableProvider(context.ErrorTemplates.Predicate, out provider);

        /// <inheritdoc />
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            context.ValidationContext.ErrorTemplates.Predicate.ProvideMessage(in context);
    }
}
