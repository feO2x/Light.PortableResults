using System;
using System.Diagnostics.CodeAnalysis;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Messaging;
using Light.PortableResults.Validation.Targeting;

namespace Light.PortableResults.Validation.Definitions;

/// <summary>
/// Represents an immutable reusable validation rule definition.
/// </summary>
public abstract class ValidationErrorDefinition
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationErrorDefinition" />.
    /// </summary>
    /// <param name="code">The default error code.</param>
    /// <param name="metadata">The default error metadata.</param>
    /// <param name="target">The default error target.</param>
    /// <param name="category">The default error category.</param>
    protected ValidationErrorDefinition(
        string? code = null,
        MetadataObject? metadata = null,
        ValidationTarget? target = null,
        ErrorCategory category = ErrorCategory.Validation
    )
    {
        Code = code;
        Metadata = metadata;
        Target = target;
        Category = category;
    }

    /// <summary>
    /// Gets the default error code.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Gets the default error metadata.
    /// </summary>
    public MetadataObject? Metadata { get; }

    /// <summary>
    /// Gets the default error target.
    /// </summary>
    public ValidationTarget? Target { get; }

    /// <summary>
    /// Gets the default error category.
    /// </summary>
    public ErrorCategory Category { get; }

    /// <summary>
    /// Gets a value indicating whether the produced message depends only on the display name and fixed parameters.
    /// </summary>
    public virtual bool IsMessageStable => false;

    /// <summary>
    /// Tries to get the effective provider that determines a stable message for the specified context.
    /// </summary>
    /// <param name="context">The readonly validation context.</param>
    /// <param name="provider">The effective message provider when the message is stable.</param>
    /// <returns>
    /// <see langword="true" /> when the message is stable for the current context and can participate in caching;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public virtual bool TryGetStableMessageProvider(
        ReadOnlyValidationContext context,
        [NotNullWhen(true)] out object? provider
    )
    {
        provider = null;
        return false;
    }

    /// <summary>
    /// Creates the validation error message for the specified check context.
    /// </summary>
    /// <typeparam name="T">The checked value type.</typeparam>
    /// <param name="context">The message context.</param>
    /// <returns>The created validation error message.</returns>
    public abstract ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context);
}

/// <summary>
/// Represents an immutable reusable validation rule definition with a typed parameter.
/// </summary>
/// <typeparam name="TParameter">The parameter type.</typeparam>
public abstract class ValidationErrorDefinition<TParameter> : ValidationErrorDefinition
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationErrorDefinition{TParameter}" />.
    /// </summary>
    /// <param name="parameter">The fixed rule parameter.</param>
    /// <param name="code">The default error code.</param>
    /// <param name="metadata">The default error metadata.</param>
    /// <param name="target">The default error target.</param>
    /// <param name="category">The default error category.</param>
    protected ValidationErrorDefinition(
        TParameter parameter,
        string? code = null,
        MetadataObject? metadata = null,
        ValidationTarget? target = null,
        ErrorCategory category = ErrorCategory.Validation
    )
        : base(code, metadata, target, category)
    {
        Parameter = parameter;
    }

    /// <summary>
    /// Gets the fixed rule parameter.
    /// </summary>
    public TParameter Parameter { get; }
}

/// <summary>
/// A validation error definition backed by a reusable message template.
/// </summary>
public class TemplateValidationErrorDefinition : ValidationErrorDefinition
{
    /// <summary>
    /// Initializes a new instance of <see cref="TemplateValidationErrorDefinition" />.
    /// </summary>
    /// <param name="template">The reusable message template.</param>
    /// <param name="code">The default error code.</param>
    /// <param name="metadata">The default error metadata.</param>
    /// <param name="target">The default error target.</param>
    /// <param name="category">The default error category.</param>
    public TemplateValidationErrorDefinition(
        IValidationErrorMessageTemplate template,
        string? code = null,
        MetadataObject? metadata = null,
        ValidationTarget? target = null,
        ErrorCategory category = ErrorCategory.Validation
    )
        : base(code, metadata, target, category)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
    }

    /// <summary>
    /// Gets the reusable message template.
    /// </summary>
    public IValidationErrorMessageTemplate Template { get; }

    /// <inheritdoc />
    public override bool IsMessageStable => Template.IsMessageStable;

    /// <inheritdoc />
    public override bool TryGetStableMessageProvider(
        ReadOnlyValidationContext context,
        [NotNullWhen(true)] out object? provider
    )
    {
        if (!Template.IsMessageStable)
        {
            provider = null;
            return false;
        }

        provider = Template;
        return true;
    }

    /// <inheritdoc />
    public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
        Template.ProvideMessage(in context);
}

/// <summary>
/// A parameterized validation error definition backed by a reusable message template.
/// </summary>
/// <typeparam name="TParameter">The parameter type.</typeparam>
public class TemplateValidationErrorDefinition<TParameter> : ValidationErrorDefinition<TParameter>
{
    /// <summary>
    /// Initializes a new instance of <see cref="TemplateValidationErrorDefinition{TParameter}" />.
    /// </summary>
    /// <param name="template">The reusable message template.</param>
    /// <param name="parameter">The fixed rule parameter.</param>
    /// <param name="code">The default error code.</param>
    /// <param name="metadata">The default error metadata.</param>
    /// <param name="target">The default error target.</param>
    /// <param name="category">The default error category.</param>
    public TemplateValidationErrorDefinition(
        IValidationErrorMessageTemplate<TParameter> template,
        TParameter parameter,
        string? code = null,
        MetadataObject? metadata = null,
        ValidationTarget? target = null,
        ErrorCategory category = ErrorCategory.Validation
    )
        : base(parameter, code, metadata, target, category)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
    }

    /// <summary>
    /// Gets the reusable message template.
    /// </summary>
    public IValidationErrorMessageTemplate<TParameter> Template { get; }

    /// <inheritdoc />
    public override bool IsMessageStable => Template.IsMessageStable;

    /// <inheritdoc />
    public override bool TryGetStableMessageProvider(
        ReadOnlyValidationContext context,
        [NotNullWhen(true)] out object? provider
    )
    {
        if (!Template.IsMessageStable)
        {
            provider = null;
            return false;
        }

        provider = Template;
        return true;
    }

    /// <inheritdoc />
    public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
        Template.ProvideMessage(in context, Parameter);
}
