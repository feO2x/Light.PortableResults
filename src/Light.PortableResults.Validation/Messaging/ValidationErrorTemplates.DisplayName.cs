using System;

namespace Light.PortableResults.Validation.Messaging;

public sealed partial record ValidationErrorTemplates
{
    /// <summary>
    /// Creates a message from the display name and constant text fragments.
    /// </summary>
    public sealed class DisplayName : IValidationErrorMessageTemplate
    {
        private readonly string? _key;
        private readonly string _prefix;
        private readonly string _suffix;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayName" /> class.
        /// </summary>
        /// <param name="suffix">The suffix appended after the display name.</param>
        /// <param name="key">The optional machine-readable message key.</param>
        /// <param name="prefix">The optional prefix inserted before the display name.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="suffix" /> or <paramref name="prefix" /> is <see langword="null" />.</exception>
        public DisplayName(string suffix, string? key = null, string prefix = "")
        {
            _suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
            _key = key;
            _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        }

        /// <inheritdoc />
        public bool IsMessageStable => true;

        /// <inheritdoc />
        public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            new ($"{_prefix}{context.DisplayName}{_suffix}", _key);
    }
}
