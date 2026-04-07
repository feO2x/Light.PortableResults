using System;

namespace Light.PortableResults.Validation.Messaging;

public sealed partial record ValidationErrorTemplates
{
    /// <summary>
    /// Creates range messages by combining the display name with formatted lower and upper boundaries.
    /// </summary>
    public sealed class DisplayNameWithRange : IRangeValidationErrorMessageTemplate
    {
        private readonly string _afterUpperBoundary;
        private readonly string _beforeLowerBoundary;
        private readonly string _betweenBoundaries;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayNameWithRange" /> class.
        /// </summary>
        /// <param name="beforeLowerBoundary">The text before the lower boundary.</param>
        /// <param name="betweenBoundaries">The text between both boundaries.</param>
        /// <param name="afterUpperBoundary">The text after the upper boundary.</param>
        public DisplayNameWithRange(
            string beforeLowerBoundary,
            string betweenBoundaries,
            string afterUpperBoundary = ""
        )
        {
            _beforeLowerBoundary = beforeLowerBoundary ?? throw new ArgumentNullException(nameof(beforeLowerBoundary));
            _betweenBoundaries = betweenBoundaries ?? throw new ArgumentNullException(nameof(betweenBoundaries));
            _afterUpperBoundary = afterUpperBoundary ?? throw new ArgumentNullException(nameof(afterUpperBoundary));
        }

        /// <inheritdoc />
        public bool IsMessageStable => true;

        /// <inheritdoc />
        public ValidationErrorMessage ProvideMessage<T, TBoundary>(
            in ValidationErrorMessageContext<T> context,
            TBoundary lowerBoundary,
            TBoundary upperBoundary
        )
        {
            var validationContext = context.ValidationContext;
            var formattedLowerBoundary =
                ValidationErrorMessageFormatting.FormatParameter(lowerBoundary, validationContext);
            var formattedUpperBoundary =
                ValidationErrorMessageFormatting.FormatParameter(upperBoundary, validationContext);
            return new ValidationErrorMessage(
                context.DisplayName +
                _beforeLowerBoundary +
                formattedLowerBoundary +
                _betweenBoundaries +
                formattedUpperBoundary +
                _afterUpperBoundary
            );
        }
    }
}
