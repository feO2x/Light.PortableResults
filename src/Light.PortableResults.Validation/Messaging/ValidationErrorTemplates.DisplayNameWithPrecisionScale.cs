using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation.Messaging;

public sealed partial record ValidationErrorTemplates
{
    /// <summary>
    /// Creates a message for decimal precision-and-scale validation failures.
    /// </summary>
    public sealed class DisplayNameWithPrecisionScale : IValidationErrorMessageTemplate<PrecisionScaleDescriptor>
    {
        /// <inheritdoc />
        public bool IsMessageStable => true;

        /// <inheritdoc />
        public ValidationErrorMessage ProvideMessage<T>(
            in ValidationErrorMessageContext<T> context,
            PrecisionScaleDescriptor parameter
        )
        {
            var trailingZeroText = parameter.IgnoreTrailingZeros ?
                " when trailing decimal zeros are ignored" :
                string.Empty;
            return new ValidationErrorMessage(
                $"{context.DisplayName} must not be more than {parameter.Precision} digits in total, with allowance for {parameter.Scale} decimals{trailingZeroText}"
            );
        }
    }
}
