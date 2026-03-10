using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Represents the mutable state of a validation operation, tracking validation errors
/// and providing configuration options. This class uses an optimized storage strategy
/// that minimizes allocations for common scenarios (0-1 errors, 2-10 errors, and more than 10 errors).
/// </summary>
public sealed class ValidationState
{
    private const int InitialErrorCapacity = 10;

    private Error[]? _errors;
    private Error _firstError;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationState" /> class.
    /// </summary>
    /// <param name="options">The validation context options.</param>
    /// <param name="errorTemplates">The error templates for generating validation errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> or <paramref name="errorTemplates" /> is null.</exception>
    public ValidationState(ValidationContextOptions options, ValidationErrorTemplates errorTemplates)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        ErrorTemplates = errorTemplates ?? throw new ArgumentNullException(nameof(errorTemplates));
    }

    /// <summary>
    /// Gets the validation context options that control validation behavior.
    /// </summary>
    public ValidationContextOptions Options { get; }

    /// <summary>
    /// Gets the error templates used for generating validation errors.
    /// </summary>
    public ValidationErrorTemplates ErrorTemplates { get; }

    /// <summary>
    /// Gets the total number of validation errors that have been added.
    /// </summary>
    public int ErrorCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether any validation errors have been added.
    /// </summary>
    public bool HasErrors => ErrorCount > 0;

    /// <summary>
    /// Adds a validation error to the state. The first error is stored inline,
    /// and subsequent errors trigger array allocation with automatic capacity growth.
    /// </summary>
    /// <param name="error">The error to add.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="error" /> is the default instance.</exception>
    public void AddError(Error error)
    {
        if (error.IsDefaultInstance)
        {
            throw new ArgumentException("The error must not be the default instance.", nameof(error));
        }

        switch (ErrorCount)
        {
            case 0:
                _firstError = error;
                ErrorCount = 1;
                return;
            case 1:
                var errors = new Error[InitialErrorCapacity];
                errors[0] = _firstError;
                errors[1] = error;
                _errors = errors;
                ErrorCount = 2;
                return;
            default:
                EnsureCapacity(ErrorCount + 1);
                _errors![ErrorCount] = error;
                ErrorCount++;
                return;
        }
    }

    /// <summary>
    /// Attempts to build an <see cref="Errors" /> collection from the accumulated validation errors.
    /// </summary>
    /// <param name="errors">When this method returns, contains the errors collection if any errors exist; otherwise, the default value.</param>
    /// <returns><c>true</c> if one or more errors exist; otherwise, <c>false</c>.</returns>
    public bool TryBuildErrors(out Errors errors)
    {
        switch (ErrorCount)
        {
            case 0:
                errors = default;
                return false;
            case 1:
                errors = new Errors(_firstError);
                return true;
            default:
                errors = new Errors(_errors!.AsMemory(0, ErrorCount));
                return true;
        }
    }

    private void EnsureCapacity(int requiredCount)
    {
        if (_errors is not null && _errors.Length >= requiredCount)
        {
            return;
        }

        var currentCapacity = _errors?.Length ?? 0;
        var newCapacity = currentCapacity == 0 ?
            InitialErrorCapacity :
            Math.Max(currentCapacity * 2, requiredCount);
        var newErrors = new Error[newCapacity];
        if (_errors is not null)
        {
            Array.Copy(_errors, newErrors, ErrorCount);
        }
        else if (ErrorCount > 0)
        {
            newErrors[0] = _firstError;
        }

        _errors = newErrors;
    }
}
