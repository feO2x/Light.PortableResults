using System;
using System.Runtime.CompilerServices;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.Validation;

/// <summary>
/// Tracks validation failures for a single validation run and exposes low-overhead helpers to create checks and
/// materialize flat validation errors.
/// </summary>
public sealed class ValidationContext
{
    internal ValidationContext(
        IValidationContextFactory factory,
        ValidationContextOptions options,
        ValidationErrorTemplates errorTemplates,
        ValidationErrorSink sink,
        string targetPrefix
    )
    {
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        ErrorTemplates = errorTemplates ?? throw new ArgumentNullException(nameof(errorTemplates));
        Sink = sink ?? throw new ArgumentNullException(nameof(sink));
        TargetPrefix = targetPrefix ?? throw new ArgumentNullException(nameof(targetPrefix));
    }

    /// <summary>
    /// Gets the factory that created this context.
    /// </summary>
    public IValidationContextFactory Factory { get; }

    /// <summary>
    /// Gets the options applied by this context.
    /// </summary>
    public ValidationContextOptions Options { get; }

    /// <summary>
    /// Gets the validation error templates used by this context.
    /// </summary>
    public ValidationErrorTemplates ErrorTemplates { get; }

    /// <summary>
    /// Gets the target prefix that is prepended to errors created within this context.
    /// </summary>
    public string TargetPrefix { get; }

    /// <summary>
    /// Gets a value indicating whether this context has accumulated validation failures.
    /// </summary>
    public bool HasErrors => Sink.HasErrors;

    internal ValidationErrorSink Sink { get; }

    /// <summary>
    /// Creates a child validation context that shares this context's flat error sink.
    /// </summary>
    /// <typeparam name="T">The type of the child value.</typeparam>
    /// <param name="childValue">The child value whose caller expression identifies the target prefix.</param>
    /// <param name="targetPrefix">The raw caller expression for the child value.</param>
    /// <returns>The child validation context.</returns>
    public ValidationContext CreateChildContext<T>(
        T childValue,
        [CallerArgumentExpression("childValue")] string targetPrefix = ""
    ) => Factory.CreateChildValidationContext(this, childValue, targetPrefix);

    /// <summary>
    /// Creates a child validation context with the specified target prefix.
    /// </summary>
    /// <param name="targetPrefix">The target prefix for the new child scope.</param>
    /// <param name="isTargetPrefixNormalized">
    /// <see langword="true" /> when <paramref name="targetPrefix" /> is already normalized; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>The child validation context.</returns>
    public ValidationContext CreateChildContext(string targetPrefix, bool isTargetPrefixNormalized = false) =>
        Factory.CreateChildValidationContext(this, targetPrefix, isTargetPrefixNormalized);

    /// <summary>
    /// Creates a check for the specified value and raw caller expression target.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="normalizeStringValue">
    /// Overrides the context-wide string normalization behavior for this specific check when set.
    /// </param>
    /// <param name="target">The raw target expression.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The created check.</returns>
    public Check<T> Check<T>(
        T value,
        bool? normalizeStringValue = null,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    )
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        displayName ??= target;
        if (ShouldNormalizeStringValue(normalizeStringValue))
        {
            value = NormalizeValueIfNecessary(value);
        }

        return new Check<T>(this, target, displayName, value, isTargetNormalized: false, isShortCircuited: false);
    }

    /// <summary>
    /// Adds the specified validation error to this context.
    /// </summary>
    /// <param name="error">The error to add.</param>
    public void AddError(Error error)
    {
        if (error.IsDefaultInstance)
        {
            throw new ArgumentException("The error must not be the default instance.", nameof(error));
        }

        if (error.Target is null && TargetPrefix.Length > 0)
        {
            error = error with { Target = TargetPrefix };
        }

        Sink.Add(error);
    }

    /// <summary>
    /// Creates and adds a validation error with the specified message and optional details.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The optional error code.</param>
    /// <param name="target">The optional target path.</param>
    /// <param name="metadata">The optional error metadata.</param>
    public void AddError(string message, string? code = null, string? target = null, MetadataObject? metadata = null)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (target is null)
        {
            target = TargetPrefix;
        }
        else
        {
            target = ComposeTarget(target, isTargetNormalized: true);
        }

        AddError(
            new Error
            {
                Message = message,
                Code = code,
                Target = target,
                Category = ErrorCategory.Validation,
                Metadata = metadata
            }
        );
    }

    /// <summary>
    /// Normalizes a raw target expression using the configured target normalizer.
    /// </summary>
    /// <param name="rawTarget">The raw target expression.</param>
    /// <returns>The normalized target.</returns>
    public string NormalizeTarget(string rawTarget) => Options.TargetNormalizer.Normalize(rawTarget);

    /// <summary>
    /// Normalizes a string value using the configured string normalization behavior.
    /// </summary>
    /// <param name="value">The string value to normalize.</param>
    /// <returns>The normalized string value.</returns>
    public string NormalizeStringValue(string? value) => Options.NormalizeStringValue(value);

    /// <summary>
    /// Creates the automatic null-validation error for the specified target and display name.
    /// </summary>
    /// <param name="target">The normalized target.</param>
    /// <param name="displayName">The display name.</param>
    /// <returns>The created error.</returns>
    public Error CreateErrorForAutomaticNullCheck(string target, string displayName)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (displayName is null)
        {
            throw new ArgumentNullException(nameof(displayName));
        }

        return Options.CreateAutomaticNullError?.Invoke(this, target, displayName) ??
               new Error
               {
                   Message = ErrorTemplates.Format(ErrorTemplates.NotNull, displayName),
                   Code = "NotNull",
                   Target = target,
                   Category = ErrorCategory.Validation
               };
    }

    /// <summary>
    /// Tries to materialize the accumulated errors.
    /// </summary>
    /// <param name="errors">The accumulated errors when present.</param>
    /// <returns><see langword="true" /> when errors are present; otherwise, <see langword="false" />.</returns>
    public bool TryGetErrors(out Errors errors) => Sink.TryBuildErrors(out errors);

    /// <summary>
    /// Materializes the accumulated errors.
    /// </summary>
    /// <returns>The accumulated errors or the empty <see cref="Errors" /> value on success.</returns>
    public Errors ToErrors() => TryGetErrors(out var errors) ? errors : default;

    /// <summary>
    /// Materializes the accumulated errors as a failure <see cref="Result" />.
    /// </summary>
    /// <returns>The failure result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the context contains no validation failures.</exception>
    public Result ToFailureResult()
    {
        if (!TryGetErrors(out var errors))
        {
            throw new InvalidOperationException(
                "Cannot create a failure result when no validation errors are present."
            );
        }

        return Result.Fail(errors);
    }

    internal bool ShouldCreateAutomaticNullError() => Options.CreateAutomaticNullErrors;

    internal string GetAutomaticNullTarget(string rawTarget)
    {
        if (TargetPrefix.Length > 0)
        {
            if (ValidationTargets.IsSimpleIdentifier(rawTarget))
            {
                return TargetPrefix;
            }

            var normalizedTarget = NormalizeTarget(rawTarget);
            if (string.Equals(normalizedTarget, TargetPrefix, StringComparison.Ordinal))
            {
                return TargetPrefix;
            }
        }

        if (TargetPrefix.Length == 0 && ValidationTargets.IsSimpleIdentifier(rawTarget))
        {
            return string.Empty;
        }

        return ComposeTarget(rawTarget, isTargetNormalized: false);
    }

    internal string ComposeTarget(string target, bool isTargetNormalized)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var normalizedTarget = isTargetNormalized ? target : NormalizeTarget(target);
        return ValidationTargets.Compose(TargetPrefix, normalizedTarget);
    }

    private bool ShouldNormalizeStringValue(bool? overrideValue) => overrideValue ?? Options.NormalizeStringValues;

    private T NormalizeValueIfNecessary<T>(T value)
    {
        if (typeof(T) == typeof(string))
        {
            var normalizedString = NormalizeStringValue((string?) (object?) value);
            return (T) (object) normalizedString;
        }

        if (value is string stringValue)
        {
            return (T) (object) NormalizeStringValue(stringValue);
        }

        return value;
    }
}
