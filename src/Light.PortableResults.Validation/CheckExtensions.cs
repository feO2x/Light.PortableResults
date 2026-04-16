using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults.Validation.Targeting;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides extension methods for <see cref="Check{T}" />.
/// </summary>
public static class CheckExtensions
{
    /// <summary>
    /// Validates the current check value with a child validator that returns the same type.
    /// </summary>
    /// <typeparam name="T">The type of the value being validated.</typeparam>
    /// <param name="check">The check instance supplying the value and context.</param>
    /// <param name="childValidator">The validator responsible for validating the child value.</param>
    /// <returns>The validated child value or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="childValidator" /> is <see langword="null" />.</exception>
    public static ValidatedValue<T> ValidateChild<T>(
        this Check<T> check,
        Validator<T> childValidator
    )
    {
        if (childValidator is null)
        {
            throw new ArgumentNullException(nameof(childValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<T>.NoValue;
        }

        return childValidator.ValidateChildValue(
            check.Value,
            check.CreateChildContext(),
            check.GetResolvedAbsoluteTargetDescriptor(),
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
    /// <returns>The validated child value or no value when validation failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="childValidator" /> is <see langword="null" />.</exception>
    public static ValidatedValue<TValidated> ValidateChild<TSource, TValidated>(
        this Check<TSource> check,
        Validator<TSource, TValidated> childValidator
    )
    {
        if (childValidator is null)
        {
            throw new ArgumentNullException(nameof(childValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<TValidated>.NoValue;
        }

        return childValidator.ValidateChildValue(
            check.Value,
            check.CreateChildContext(),
            check.GetResolvedAbsoluteTargetDescriptor(),
            check.DisplayName
        );
    }

    /// <summary>
    /// Validates the current check value with an asynchronous child validator that returns the same type.
    /// </summary>
    /// <typeparam name="T">The type of the value being validated.</typeparam>
    /// <param name="check">The check instance supplying the value and context.</param>
    /// <param name="childValidator">The asynchronous validator responsible for validating the child value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated child value or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="childValidator" /> is <see langword="null" />.</exception>
    public static ValueTask<ValidatedValue<T>> ValidateChildAsync<T>(
        this Check<T> check,
        AsyncValidator<T> childValidator,
        CancellationToken cancellationToken = default
    )
    {
        if (childValidator is null)
        {
            throw new ArgumentNullException(nameof(childValidator));
        }

        if (check.IsShortCircuited)
        {
            return new ValueTask<ValidatedValue<T>>(ValidatedValue<T>.NoValue);
        }

        return childValidator.ValidateChildValueAsync(
            check.Value,
            check.CreateChildContext(),
            cancellationToken,
            check.GetResolvedAbsoluteTargetDescriptor(),
            check.DisplayName
        );
    }

    /// <summary>
    /// Validates the current check value with an asynchronous child validator that maps the source type to a new
    /// validated type.
    /// </summary>
    /// <typeparam name="TSource">The type of the value held by the current check.</typeparam>
    /// <typeparam name="TValidated">The type produced by the child validator when validation succeeds.</typeparam>
    /// <param name="check">The check instance supplying the value and context.</param>
    /// <param name="childValidator">The asynchronous validator that transforms and validates the source value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated child value or no value when validation failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="childValidator" /> is <see langword="null" />.</exception>
    public static ValueTask<ValidatedValue<TValidated>> ValidateChildAsync<TSource, TValidated>(
        this Check<TSource> check,
        AsyncValidator<TSource, TValidated> childValidator,
        CancellationToken cancellationToken = default
    )
    {
        if (childValidator is null)
        {
            throw new ArgumentNullException(nameof(childValidator));
        }

        if (check.IsShortCircuited)
        {
            return new ValueTask<ValidatedValue<TValidated>>(ValidatedValue<TValidated>.NoValue);
        }

        return childValidator.ValidateChildValueAsync(
            check.Value,
            check.CreateChildContext(),
            cancellationToken,
            check.GetResolvedAbsoluteTargetDescriptor(),
            check.DisplayName
        );
    }

    /// <summary>
    /// Validates and potentially normalizes each item of a mutable indexed collection in place.
    /// </summary>
    /// <typeparam name="TCollection">The mutable collection type.</typeparam>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The validator applied to each item in the collection.</param>
    /// <returns>The validated collection or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <remarks>
    /// When the collection is <see langword="null" />, this method first asks the active automatic-null provider to
    /// create the standard null error for the collection target. Guard nullable collections explicitly, for example
    /// with <c>IsNotNull()</c>, when the active provider may decline to create an error and deterministic
    /// non-exceptional behavior is required.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection value is <see langword="null" /> and the active automatic-null provider declines to
    /// create an error. Guard nullable collections explicitly, for example with <c>IsNotNull()</c>, when
    /// deterministic non-exceptional behavior is required.
    /// </exception>
    public static ValidatedValue<TCollection> ValidateItems<TCollection, TItem>(
        this Check<TCollection> check,
        Validator<TItem> itemValidator
    )
        where TCollection : IList<TItem>
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        if (!TryGetCollectionForItemValidation(check, out var collection))
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        for (var i = 0; i < collection.Count; i++)
        {
            var itemCheckpoint = check.Context.CreateCheckpoint();
            var validatedValue = itemValidator.ValidateChildValue(
                collection[i],
                check.CreateChildContextForIndex(i),
                ValidationTarget.Relative(string.Empty, isNormalized: true),
                displayName: CreateIndexedDisplayName(check, i)
            );

            if (!itemCheckpoint.HasNewErrors)
            {
                collection[i] = validatedValue.Value;
            }
        }

        return checkpoint.ToValidatedValue(collection);
    }

    /// <summary>
    /// Validates each item of an immutable array without replacing items.
    /// Use the transforming overload when item validation should materialize a new immutable array.
    /// </summary>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The validator applied to each item in the collection.</param>
    /// <returns>The validated collection or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <see langword="null" />.</exception>
    public static ValidatedValue<ImmutableArray<TItem>> ValidateItems<TItem>(
        this Check<ImmutableArray<TItem>> check,
        Validator<TItem> itemValidator
    )
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<ImmutableArray<TItem>>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        var collection = check.Value;
        for (var i = 0; i < collection.Length; i++)
        {
            itemValidator.ValidateChildValue(
                collection[i],
                check.CreateChildContextForIndex(i),
                ValidationTarget.Relative(string.Empty, isNormalized: true),
                displayName: CreateIndexedDisplayName(check, i)
            );
        }

        return checkpoint.ToValidatedValue(collection);
    }

    /// <summary>
    /// Validates each item of a collection with an ad-hoc delegate without replacing items.
    /// This overload is intended for primitive items or lightweight collection-specific rules.
    /// </summary>
    /// <typeparam name="TCollection">The read-only collection type.</typeparam>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="validateItem">The item-validation delegate.</param>
    /// <returns>The validated collection or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <remarks>
    /// When the collection is <see langword="null" />, this method first asks the active automatic-null provider to
    /// create the standard null error for the collection target. Guard nullable collections explicitly, for example
    /// with <c>IsNotNull()</c>, when the active provider may decline to create an error and deterministic
    /// non-exceptional behavior is required.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validateItem" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection value is <see langword="null" /> and the active automatic-null provider declines to
    /// create an error. Guard nullable collections explicitly, for example with <c>IsNotNull()</c>, when
    /// deterministic non-exceptional behavior is required.
    /// </exception>
    public static ValidatedValue<TCollection> ValidateItems<TCollection, TItem>(
        this Check<TCollection> check,
        Action<Check<TItem>> validateItem
    )
        where TCollection : IReadOnlyList<TItem>
    {
        if (validateItem is null)
        {
            throw new ArgumentNullException(nameof(validateItem));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        if (!TryGetCollectionForItemValidation(check, out var collection))
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        for (var i = 0; i < collection.Count; i++)
        {
            validateItem(CreateItemCheck(check, collection[i], i));
        }

        return checkpoint.ToValidatedValue(collection);
    }

    /// <summary>
    /// Validates and potentially normalizes each item of a mutable indexed collection in place with an ad-hoc
    /// delegate.
    /// </summary>
    /// <typeparam name="TCollection">The mutable collection type.</typeparam>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="validateItem">The item-validation delegate that can return a replacement item value.</param>
    /// <returns>The validated collection or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <remarks>
    /// When the collection is <see langword="null" />, this method first asks the active automatic-null provider to
    /// create the standard null error for the collection target. Guard nullable collections explicitly, for example
    /// with <c>IsNotNull()</c>, when the active provider may decline to create an error and deterministic
    /// non-exceptional behavior is required.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validateItem" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection value is <see langword="null" /> and the active automatic-null provider declines to
    /// create an error. Guard nullable collections explicitly, for example with <c>IsNotNull()</c>, when
    /// deterministic non-exceptional behavior is required.
    /// </exception>
    public static ValidatedValue<TCollection> ValidateItems<TCollection, TItem>(
        this Check<TCollection> check,
        Func<Check<TItem>, ValidatedValue<TItem>> validateItem
    )
        where TCollection : IList<TItem>
    {
        if (validateItem is null)
        {
            throw new ArgumentNullException(nameof(validateItem));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        if (!TryGetCollectionForItemValidation(check, out var collection))
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        for (var i = 0; i < collection.Count; i++)
        {
            var itemCheckpoint = check.Context.CreateCheckpoint();
            var validatedValue = validateItem(CreateItemCheck(check, collection[i], i));
            if (!itemCheckpoint.HasNewErrors)
            {
                collection[i] = validatedValue.Value;
            }
        }

        return checkpoint.ToValidatedValue(collection);
    }

    /// <summary>
    /// Validates array items with a transforming validator and materializes a new array.
    /// For unsupported collection shapes, create a dedicated child validator for the collection itself.
    /// </summary>
    /// <typeparam name="TItem">The source item type.</typeparam>
    /// <typeparam name="TValidated">The validated item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The validator applied to each item in the collection.</param>
    /// <returns>The transformed array or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <remarks>
    /// When the array is <see langword="null" />, this method first asks the active automatic-null provider to create
    /// the standard null error for the collection target. Guard nullable collections explicitly, for example with
    /// <c>IsNotNull()</c>, when the active provider may decline to create an error and deterministic non-exceptional
    /// behavior is required.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection value is <see langword="null" /> and the active automatic-null provider declines to
    /// create an error. Guard nullable collections explicitly, for example with <c>IsNotNull()</c>, when
    /// deterministic non-exceptional behavior is required.
    /// </exception>
    public static ValidatedValue<TValidated[]> ValidateItems<TItem, TValidated>(
        this Check<TItem[]> check,
        Validator<TItem, TValidated> itemValidator
    )
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<TValidated[]>.NoValue;
        }

        if (!TryGetCollectionForItemValidation(check, out var collection))
        {
            return ValidatedValue<TValidated[]>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        var validatedItems = new TValidated[collection.Length];
        for (var i = 0; i < collection.Length; i++)
        {
            var itemCheckpoint = check.Context.CreateCheckpoint();
            var validatedValue = itemValidator.ValidateChildValue(
                collection[i],
                check.CreateChildContextForIndex(i),
                ValidationTarget.Relative(string.Empty, isNormalized: true),
                displayName: CreateIndexedDisplayName(check, i)
            );

            if (!itemCheckpoint.HasNewErrors)
            {
                validatedItems[i] = validatedValue.Value;
            }
        }

        return checkpoint.ToValidatedValue(validatedItems);
    }

    /// <summary>
    /// Validates list items with a transforming validator and materializes a new list.
    /// </summary>
    /// <typeparam name="TItem">The source item type.</typeparam>
    /// <typeparam name="TValidated">The validated item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The validator applied to each item in the collection.</param>
    /// <returns>The transformed list or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <remarks>
    /// When the list is <see langword="null" />, this method first asks the active automatic-null provider to create
    /// the standard null error for the collection target. Guard nullable collections explicitly, for example with
    /// <c>IsNotNull()</c>, when the active provider may decline to create an error and deterministic non-exceptional
    /// behavior is required.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection value is <see langword="null" /> and the active automatic-null provider declines to
    /// create an error. Guard nullable collections explicitly, for example with <c>IsNotNull()</c>, when
    /// deterministic non-exceptional behavior is required.
    /// </exception>
    public static ValidatedValue<List<TValidated>> ValidateItems<TItem, TValidated>(
        this Check<List<TItem>> check,
        Validator<TItem, TValidated> itemValidator
    )
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<List<TValidated>>.NoValue;
        }

        if (!TryGetCollectionForItemValidation(check, out var collection))
        {
            return ValidatedValue<List<TValidated>>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        var validatedItems = new List<TValidated>(collection.Count);
        for (var i = 0; i < collection.Count; i++)
        {
            var itemCheckpoint = check.Context.CreateCheckpoint();
            var validatedValue = itemValidator.ValidateChildValue(
                collection[i],
                check.CreateChildContextForIndex(i),
                ValidationTarget.Relative(string.Empty, isNormalized: true),
                displayName: CreateIndexedDisplayName(check, i)
            );

            if (!itemCheckpoint.HasNewErrors)
            {
                validatedItems.Add(validatedValue.Value);
            }
        }

        return checkpoint.ToValidatedValue(validatedItems);
    }

    /// <summary>
    /// Validates immutable-array items with a transforming validator and materializes a new immutable array.
    /// </summary>
    /// <typeparam name="TItem">The source item type.</typeparam>
    /// <typeparam name="TValidated">The validated item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The validator applied to each item in the collection.</param>
    /// <returns>The transformed immutable array or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <see langword="null" />.</exception>
    public static ValidatedValue<ImmutableArray<TValidated>> ValidateItems<TItem, TValidated>(
        this Check<ImmutableArray<TItem>> check,
        Validator<TItem, TValidated> itemValidator
    )
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<ImmutableArray<TValidated>>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        var collection = check.Value;
        var validatedItems = ImmutableArray.CreateBuilder<TValidated>(collection.Length);
        for (var i = 0; i < collection.Length; i++)
        {
            var itemCheckpoint = check.Context.CreateCheckpoint();
            var validatedValue = itemValidator.ValidateChildValue(
                collection[i],
                check.CreateChildContextForIndex(i),
                ValidationTarget.Relative(string.Empty, isNormalized: true),
                displayName: CreateIndexedDisplayName(check, i)
            );

            if (!itemCheckpoint.HasNewErrors)
            {
                validatedItems.Add(validatedValue.Value);
            }
        }

        return checkpoint.ToValidatedValue(validatedItems.DrainToImmutable());
    }

    /// <summary>
    /// Validates and potentially normalizes each item of a mutable indexed collection in place with an asynchronous
    /// validator.
    /// </summary>
    /// <typeparam name="TCollection">The mutable collection type.</typeparam>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The asynchronous validator applied to each item in the collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated collection or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <remarks>
    /// When the collection is <see langword="null" />, this method first asks the active automatic-null provider to
    /// create the standard null error for the collection target. Guard nullable collections explicitly, for example
    /// with <c>IsNotNull()</c>, when the active provider may decline to create an error and deterministic
    /// non-exceptional behavior is required.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection value is <see langword="null" /> and the active automatic-null provider declines to
    /// create an error. Guard nullable collections explicitly, for example with <c>IsNotNull()</c>, when
    /// deterministic non-exceptional behavior is required.
    /// </exception>
    public static async ValueTask<ValidatedValue<TCollection>> ValidateItemsAsync<TCollection, TItem>(
        this Check<TCollection> check,
        AsyncValidator<TItem> itemValidator,
        CancellationToken cancellationToken = default
    )
        where TCollection : IList<TItem>
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        if (!TryGetCollectionForItemValidation(check, out var collection))
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        for (var i = 0; i < collection.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var itemCheckpoint = check.Context.CreateCheckpoint();
            var validatedValue = await itemValidator.ValidateChildValueAsync(
                    collection[i],
                    check.CreateChildContextForIndex(i),
                    cancellationToken,
                    ValidationTarget.Relative(string.Empty, isNormalized: true),
                    displayName: CreateIndexedDisplayName(check, i)
                )
               .ConfigureAwait(false);

            if (!itemCheckpoint.HasNewErrors)
            {
                collection[i] = validatedValue.Value;
            }
        }

        return checkpoint.ToValidatedValue(collection);
    }

    /// <summary>
    /// Validates each item of a read-only list with an asynchronous validator without replacing items.
    /// </summary>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The asynchronous validator applied to each item in the collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated collection or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <remarks>
    /// When the collection is <see langword="null" />, this method first asks the active automatic-null provider to
    /// create the standard null error for the collection target. Guard nullable collections explicitly, for example
    /// with <c>IsNotNull()</c>, when the active provider may decline to create an error and deterministic
    /// non-exceptional behavior is required.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection value is <see langword="null" /> and the active automatic-null provider declines to
    /// create an error. Guard nullable collections explicitly, for example with <c>IsNotNull()</c>, when
    /// deterministic non-exceptional behavior is required.
    /// </exception>
    public static async ValueTask<ValidatedValue<IReadOnlyList<TItem>>> ValidateItemsAsync<TItem>(
        this Check<IReadOnlyList<TItem>> check,
        AsyncValidator<TItem> itemValidator,
        CancellationToken cancellationToken = default
    )
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<IReadOnlyList<TItem>>.NoValue;
        }

        if (!TryGetCollectionForItemValidation(check, out var collection))
        {
            return ValidatedValue<IReadOnlyList<TItem>>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        for (var i = 0; i < collection.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await itemValidator.ValidateChildValueAsync(
                    collection[i],
                    check.CreateChildContextForIndex(i),
                    cancellationToken,
                    ValidationTarget.Relative(string.Empty, isNormalized: true),
                    displayName: CreateIndexedDisplayName(check, i)
                )
               .ConfigureAwait(false);
        }

        return checkpoint.ToValidatedValue(collection);
    }

    /// <summary>
    /// Validates each item of an immutable array with an asynchronous validator without replacing items.
    /// </summary>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The asynchronous validator applied to each item in the collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated collection or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <see langword="null" />.</exception>
    public static async ValueTask<ValidatedValue<ImmutableArray<TItem>>> ValidateItemsAsync<TItem>(
        this Check<ImmutableArray<TItem>> check,
        AsyncValidator<TItem> itemValidator,
        CancellationToken cancellationToken = default
    )
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<ImmutableArray<TItem>>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        var collection = check.Value;
        for (var i = 0; i < collection.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await itemValidator.ValidateChildValueAsync(
                    collection[i],
                    check.CreateChildContextForIndex(i),
                    cancellationToken,
                    ValidationTarget.Relative(string.Empty, isNormalized: true),
                    displayName: CreateIndexedDisplayName(check, i)
                )
               .ConfigureAwait(false);
        }

        return checkpoint.ToValidatedValue(collection);
    }

    /// <summary>
    /// Validates each item of a collection with an asynchronous ad-hoc delegate without replacing items.
    /// </summary>
    /// <typeparam name="TCollection">The read-only collection type.</typeparam>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="validateItem">The asynchronous item-validation delegate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated collection or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <remarks>
    /// When the collection is <see langword="null" />, this method first asks the active automatic-null provider to
    /// create the standard null error for the collection target. Guard nullable collections explicitly, for example
    /// with <c>IsNotNull()</c>, when the active provider may decline to create an error and deterministic
    /// non-exceptional behavior is required.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validateItem" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection value is <see langword="null" /> and the active automatic-null provider declines to
    /// create an error. Guard nullable collections explicitly, for example with <c>IsNotNull()</c>, when
    /// deterministic non-exceptional behavior is required.
    /// </exception>
    public static async ValueTask<ValidatedValue<TCollection>> ValidateItemsAsync<TCollection, TItem>(
        this Check<TCollection> check,
        Func<Check<TItem>, CancellationToken, ValueTask> validateItem,
        CancellationToken cancellationToken = default
    )
        where TCollection : IReadOnlyList<TItem>
    {
        if (validateItem is null)
        {
            throw new ArgumentNullException(nameof(validateItem));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        if (!TryGetCollectionForItemValidation(check, out var collection))
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        for (var i = 0; i < collection.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await validateItem(CreateItemCheck(check, collection[i], i), cancellationToken).ConfigureAwait(false);
        }

        return checkpoint.ToValidatedValue(collection);
    }

    /// <summary>
    /// Validates and potentially normalizes each item of a mutable indexed collection in place with an asynchronous
    /// ad-hoc delegate.
    /// </summary>
    /// <typeparam name="TCollection">The mutable collection type.</typeparam>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="validateItem">The asynchronous item-validation delegate that can return a replacement item value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated collection or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <remarks>
    /// When the collection is <see langword="null" />, this method first asks the active automatic-null provider to
    /// create the standard null error for the collection target. Guard nullable collections explicitly, for example
    /// with <c>IsNotNull()</c>, when the active provider may decline to create an error and deterministic
    /// non-exceptional behavior is required.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validateItem" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection value is <see langword="null" /> and the active automatic-null provider declines to
    /// create an error. Guard nullable collections explicitly, for example with <c>IsNotNull()</c>, when
    /// deterministic non-exceptional behavior is required.
    /// </exception>
    public static async ValueTask<ValidatedValue<TCollection>> ValidateItemsAsync<TCollection, TItem>(
        this Check<TCollection> check,
        Func<Check<TItem>, CancellationToken, ValueTask<ValidatedValue<TItem>>> validateItem,
        CancellationToken cancellationToken = default
    )
        where TCollection : IList<TItem>
    {
        if (validateItem is null)
        {
            throw new ArgumentNullException(nameof(validateItem));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        if (!TryGetCollectionForItemValidation(check, out var collection))
        {
            return ValidatedValue<TCollection>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        for (var i = 0; i < collection.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var itemCheckpoint = check.Context.CreateCheckpoint();
            var validatedValue =
                await validateItem(CreateItemCheck(check, collection[i], i), cancellationToken).ConfigureAwait(false);
            if (!itemCheckpoint.HasNewErrors)
            {
                collection[i] = validatedValue.Value;
            }
        }

        return checkpoint.ToValidatedValue(collection);
    }

    /// <summary>
    /// Validates array items with an asynchronous transforming validator and materializes a new array.
    /// For unsupported collection shapes, create a dedicated child validator for the collection itself.
    /// </summary>
    /// <typeparam name="TItem">The source item type.</typeparam>
    /// <typeparam name="TValidated">The validated item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The asynchronous validator applied to each item in the collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transformed array or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <remarks>
    /// When the array is <see langword="null" />, this method first asks the active automatic-null provider to create
    /// the standard null error for the collection target. Guard nullable collections explicitly, for example with
    /// <c>IsNotNull()</c>, when the active provider may decline to create an error and deterministic non-exceptional
    /// behavior is required.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection value is <see langword="null" /> and the active automatic-null provider declines to
    /// create an error. Guard nullable collections explicitly, for example with <c>IsNotNull()</c>, when
    /// deterministic non-exceptional behavior is required.
    /// </exception>
    public static async ValueTask<ValidatedValue<TValidated[]>> ValidateItemsAsync<TItem, TValidated>(
        this Check<TItem[]> check,
        AsyncValidator<TItem, TValidated> itemValidator,
        CancellationToken cancellationToken = default
    )
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<TValidated[]>.NoValue;
        }

        if (!TryGetCollectionForItemValidation(check, out var collection))
        {
            return ValidatedValue<TValidated[]>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        var validatedItems = new TValidated[collection.Length];
        for (var i = 0; i < collection.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var itemCheckpoint = check.Context.CreateCheckpoint();
            var validatedValue = await itemValidator.ValidateChildValueAsync(
                    collection[i],
                    check.CreateChildContextForIndex(i),
                    cancellationToken,
                    ValidationTarget.Relative(string.Empty, isNormalized: true),
                    displayName: CreateIndexedDisplayName(check, i)
                )
               .ConfigureAwait(false);

            if (!itemCheckpoint.HasNewErrors)
            {
                validatedItems[i] = validatedValue.Value;
            }
        }

        return checkpoint.ToValidatedValue(validatedItems);
    }

    /// <summary>
    /// Validates list items with an asynchronous transforming validator and materializes a new list.
    /// </summary>
    /// <typeparam name="TItem">The source item type.</typeparam>
    /// <typeparam name="TValidated">The validated item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The asynchronous validator applied to each item in the collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transformed list or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <remarks>
    /// When the list is <see langword="null" />, this method first asks the active automatic-null provider to create
    /// the standard null error for the collection target. Guard nullable collections explicitly, for example with
    /// <c>IsNotNull()</c>, when the active provider may decline to create an error and deterministic non-exceptional
    /// behavior is required.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the collection value is <see langword="null" /> and the active automatic-null provider declines to
    /// create an error. Guard nullable collections explicitly, for example with <c>IsNotNull()</c>, when
    /// deterministic non-exceptional behavior is required.
    /// </exception>
    public static async ValueTask<ValidatedValue<List<TValidated>>> ValidateItemsAsync<TItem, TValidated>(
        this Check<List<TItem>> check,
        AsyncValidator<TItem, TValidated> itemValidator,
        CancellationToken cancellationToken = default
    )
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<List<TValidated>>.NoValue;
        }

        if (!TryGetCollectionForItemValidation(check, out var collection))
        {
            return ValidatedValue<List<TValidated>>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        var validatedItems = new List<TValidated>(collection.Count);
        for (var i = 0; i < collection.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var itemCheckpoint = check.Context.CreateCheckpoint();
            var validatedValue = await itemValidator.ValidateChildValueAsync(
                    collection[i],
                    check.CreateChildContextForIndex(i),
                    cancellationToken,
                    ValidationTarget.Relative(string.Empty, isNormalized: true),
                    displayName: CreateIndexedDisplayName(check, i)
                )
               .ConfigureAwait(false);

            if (!itemCheckpoint.HasNewErrors)
            {
                validatedItems.Add(validatedValue.Value);
            }
        }

        return checkpoint.ToValidatedValue(validatedItems);
    }

    /// <summary>
    /// Validates immutable-array items with an asynchronous transforming validator and materializes a new immutable
    /// array.
    /// </summary>
    /// <typeparam name="TItem">The source item type.</typeparam>
    /// <typeparam name="TValidated">The validated item type.</typeparam>
    /// <param name="check">The check that provides the collection value and validation context.</param>
    /// <param name="itemValidator">The asynchronous validator applied to each item in the collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transformed immutable array or <see cref="ValidatedValue{T}.NoValue" /> when validation failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemValidator" /> is <see langword="null" />.</exception>
    public static async ValueTask<ValidatedValue<ImmutableArray<TValidated>>> ValidateItemsAsync<TItem, TValidated>(
        this Check<ImmutableArray<TItem>> check,
        AsyncValidator<TItem, TValidated> itemValidator,
        CancellationToken cancellationToken = default
    )
    {
        if (itemValidator is null)
        {
            throw new ArgumentNullException(nameof(itemValidator));
        }

        if (check.IsShortCircuited)
        {
            return ValidatedValue<ImmutableArray<TValidated>>.NoValue;
        }

        var checkpoint = check.Context.CreateCheckpoint();
        var collection = check.Value;
        var validatedItems = ImmutableArray.CreateBuilder<TValidated>(collection.Length);
        for (var i = 0; i < collection.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var itemCheckpoint = check.Context.CreateCheckpoint();
            var validatedValue = await itemValidator.ValidateChildValueAsync(
                    collection[i],
                    check.CreateChildContextForIndex(i),
                    cancellationToken,
                    ValidationTarget.Relative(string.Empty, isNormalized: true),
                    displayName: CreateIndexedDisplayName(check, i)
                )
               .ConfigureAwait(false);

            if (!itemCheckpoint.HasNewErrors)
            {
                validatedItems.Add(validatedValue.Value);
            }
        }

        return checkpoint.ToValidatedValue(validatedItems.DrainToImmutable());
    }

    private static bool TryGetCollectionForItemValidation<TCollection>(
        Check<TCollection> check,
        [NotNullWhen(true)] out TCollection? collection
    )
    {
        collection = check.Value;
        if (collection is not null)
        {
            return true;
        }

        var displayName = check.DisplayName ?? check.Target;

        // Pass the raw TargetDescriptor (not the resolved absolute target) so that
        // GetAutomaticNullTarget can apply the caller-expression special case,
        // which collapses a simple identifier to the context prefix (e.g. "request"
        // rather than "request.nullableListTags"). Using GetResolvedAbsoluteTargetDescriptor()
        // instead would bypass that branch and produce a fully-qualified target.
        if (check.Context.TryCreateAutomaticNullError(collection, check.TargetDescriptor, displayName, out var error))
        {
            check.Context.AddError(error);
            return false;
        }

        throw new InvalidOperationException(
            "ValidateItems and ValidateItemsAsync require a non-null collection when the active automatic-null provider does not create an error. Guard nullable collections explicitly, for example with IsNotNull(), before validating items."
        );
    }

    private static Check<TItem> CreateItemCheck<TCollection, TItem>(Check<TCollection> check, TItem item, int index) =>
        check
           .CreateChildContextForIndex(index)
           .Check(
                item,
                ValidationTarget.Relative(string.Empty, isNormalized: true),
                displayName: CreateIndexedDisplayName(check, index)
            );

    private static string CreateIndexedDisplayName<TCollection>(Check<TCollection> check, int index)
    {
        var name = check.DisplayName ?? check.Target;
        return name.Length == 0 ? $"[{index}]" : $"{name}[{index}]";
    }
}
