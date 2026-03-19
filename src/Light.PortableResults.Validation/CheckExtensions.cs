using System;
using System.Collections.Generic;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides extension methods for <see cref="Check{T}" />.
/// </summary>
public static class CheckExtensions
{
    /// <summary>
    /// Validates the current check value with a child validator that returns the same type and optionally short-circuits on errors.
    /// </summary>
    /// <typeparam name="T">The type of the value being validated.</typeparam>
    /// <param name="check">The check instance supplying the value and context.</param>
    /// <param name="childValidator">The validator responsible for validating the child value.</param>
    /// <returns>The updated check representing either the validated value or an error state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="childValidator" /> is <c>null</c>.</exception>
    public static ValidatedValue<T> ValidateChild<T>(
        this Check<T> check,
        Validator<T> childValidator
    )
    {
        if (childValidator is null)
        {
            throw new ArgumentNullException(nameof(childValidator));
        }

        var childValueContext = check.Context.ForMember(check.Target);
        return childValidator.ValidateChildValue(
            check.Value,
            childValueContext,
            check.Target,
            check.DisplayName
        );
    }

    /// <summary>
    /// Validates the current check value with a child validator that maps the source type to a new validated type.
    /// </summary>
    /// <typeparam name="TSource">The type of the value held by the current check.</typeparam>
    /// <typeparam name="TValidated">The type produced by the child validator when validation succeeds.</typeparam>
    /// <param name="check">The check instance supplying the value and context.</param>
    /// <param name="childValidator">The validator that transforms and validates the source value.</param>
    /// <returns>A new check containing the validated value or an error state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="childValidator" /> is <c>null</c>.</exception>
    public static ValidatedValue<TValidated> ValidateChild<TSource, TValidated>(
        this Check<TSource> check,
        Validator<TSource, TValidated> childValidator
    )
    {
        if (childValidator is null)
        {
            throw new ArgumentNullException(nameof(childValidator));
        }

        var childValueContext = check.Context.ForMember(check.Target);
        return childValidator.ValidateChildValue(
            check.Value,
            childValueContext,
            check.Target,
            check.DisplayName
        );
    }

    /// <summary>
    /// Validates each item within the collection held by the current check using the supplied item validator.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection being validated.</typeparam>
    /// <typeparam name="TItem">The type of the items contained in the collection.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The validator applied to each item in the collection.</param>
    /// <param name="isNullCheckingEnabled">Indicates whether the method should automatically handle <c>null</c> collection values.</param>
    /// <returns>The updated check reflecting the validated collection or error state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a null collection is encountered but an automatic null error cannot be created.</exception>
    public static ValidatedValue<TCollection> ValidateItems<TCollection, TItem>(
        this Check<TCollection> check,
        Validator<TItem> itemValidator,
        bool isNullCheckingEnabled = true
    )
        where TCollection : IList<TItem>
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        var collectionContext = check.Context.ForMember(check.Target);
        if (isNullCheckingEnabled && check.IsValueNull)
        {
            check = check.NormalizeTargetIfNecessary();

            if (collectionContext.TryCreateAutomaticNullError(
                    check.Value,
                    check.Target,
                    check.DisplayName,
                    out var error
                ))
            {
                check.AddError(error);
                return ValidatedValue<TCollection>.NoValue;
            }

            throw new InvalidOperationException("Failed to create automatic null error.");
        }

        var collection = check.Value;
        for (var i = 0; i < collection.Count; i++)
        {
            var indexContext = collectionContext.ForIndex(i);
            var validatedValue = itemValidator.ValidateChildValue(collection[i], indexContext, i.ToString());
            if (validatedValue.TryGetValue(out var normalizedValue))
            {
                collection[i] = normalizedValue;
            }
        }

        return check.Context.HasErrors ? ValidatedValue<TCollection>.NoValue : ValidatedValue.Success(collection);
    }
}
