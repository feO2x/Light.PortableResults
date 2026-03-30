using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Creates a message from the display name and an additional typed parameter.
/// </summary>
/// <typeparam name="TParameter">The parameter type.</typeparam>
public sealed class DisplayNameWithParameterValidationErrorMessageTemplate<TParameter> :
    IValidationErrorMessageTemplate<TParameter>
{
    private readonly string? _key;
    private readonly string _parameterPrefix;
    private readonly string _prefix;
    private readonly string _suffix;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayNameWithParameterValidationErrorMessageTemplate{TParameter}" /> class.
    /// </summary>
    /// <param name="parameterPrefix">The text inserted between the display name and the formatted parameter.</param>
    /// <param name="suffix">The text appended after the formatted parameter.</param>
    /// <param name="key">The optional machine-readable message key.</param>
    /// <param name="prefix">The optional prefix inserted before the display name.</param>
    public DisplayNameWithParameterValidationErrorMessageTemplate(
        string parameterPrefix,
        string suffix,
        string? key = null,
        string prefix = ""
    )
    {
        _parameterPrefix = parameterPrefix ?? throw new ArgumentNullException(nameof(parameterPrefix));
        _suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
        _key = key;
        _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
    }

    /// <inheritdoc />
    public bool IsMessageStable => true;

    /// <inheritdoc />
    public ValidationErrorMessage ProvideMessage<T>(
        in ValidationErrorMessageContext<T> context,
        TParameter parameter
    )
    {
        var formattedParameter = ValidationErrorMessageFormatting.FormatParameter(parameter, context.ValidationContext);
        return new ValidationErrorMessage(
            $"{_prefix}{context.DisplayName}{_parameterPrefix}{formattedParameter}{_suffix}",
            _key
        );
    }
}
