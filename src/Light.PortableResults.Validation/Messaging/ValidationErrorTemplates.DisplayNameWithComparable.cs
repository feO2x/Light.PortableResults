using System;

namespace Light.PortableResults.Validation.Messaging;

public sealed partial record ValidationErrorTemplates
{
    /// <summary>
    /// Creates comparison messages by combining the display name with a formatted boundary value.
    /// </summary>
    public sealed class DisplayNameWithComparable : IComparableValidationErrorMessageTemplate
    {
        private readonly string _prefix;
        private readonly string _suffix;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayNameWithComparable" /> class.
        /// </summary>
        /// <param name="prefix">The text inserted between the display name and the boundary.</param>
        /// <param name="suffix">The optional trailing text.</param>
        public DisplayNameWithComparable(string prefix, string suffix = "")
        {
            _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
            _suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
        }

        /// <inheritdoc />
        public bool IsMessageStable => true;

        /// <inheritdoc />
        public ValidationErrorMessage ProvideMessage<T, TParameter>(
            in ValidationErrorMessageContext<T> context,
            TParameter parameter
        )
        {
            var formattedParameter =
                ValidationErrorMessageFormatting.FormatParameter(parameter, context.ValidationContext);
            return new ValidationErrorMessage(context.DisplayName + _prefix + formattedParameter + _suffix);
        }
    }
}
