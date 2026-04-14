using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Light.PortableResults.Validation.ConfigurationIntegration;

/// <summary>
/// Provides a bridge between Light.PortableResults.Validation and Microsoft.Extensions.Options by implementing
/// <see cref="IValidateOptions{TOptions}" /> to delegate validation to a <see cref="Validator{TOptions}" />.
/// </summary>
/// <typeparam name="TOptions">The options type to validate.</typeparam>
public sealed class PortableResultsValidateOptions<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    private readonly string? _name;
    private readonly Validator<TOptions> _validator;

    /// <summary>
    /// Initializes a new instance of <see cref="PortableResultsValidateOptions{TOptions}" />.
    /// </summary>
    /// <param name="validator">The validator to use for validation.</param>
    /// <param name="name">The optional options name to filter on. When specified, validation only occurs for matching named options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validator" /> is null.</exception>
    public PortableResultsValidateOptions(Validator<TOptions> validator, string? name = null)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _name = name;
    }

    /// <summary>
    /// Validates the specified options instance.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>
    /// <see cref="ValidateOptionsResult.Success" /> when validation passes;
    /// <see cref="ValidateOptionsResult.Skip" /> when the adapter was constructed with a non-null name that does not match;
    /// or <see cref="ValidateOptionsResult.Fail(IEnumerable{string})" /> when validation fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is null.</exception>
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        // Filter by name if the adapter was constructed with a specific name
        if (_name is not null && !string.Equals(_name, name, StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Skip;
        }

        // Create a fresh validation context from the validator's factory
        var context = _validator.ValidationContextFactory.CreateValidationContext();

        // Store the options name in the context for validators that need it
        if (name is not null)
        {
            context.SetItem(ConfigurationConstants.OptionsNameKey, name);
        }

        // Validate
        if (_validator.CheckForErrors(options, context, out var failure))
        {
            var messages = new List<string>(failure.Errors.Count);
            foreach (var error in failure.Errors)
            {
                messages.Add(error.Message);
            }

            return ValidateOptionsResult.Fail(messages);
        }

        return ValidateOptionsResult.Success;
    }
}
