using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides thread-safe reuse of immutable validation error definitions.
/// </summary>
public interface IValidationErrorDefinitionCache
{
    /// <summary>
    /// Gets an existing validation error definition for the specified key or creates and stores a new one.
    /// </summary>
    /// <typeparam name="TKey">The cache key type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="key">The effective rule-identity key.</param>
    /// <param name="factory">The factory used when the definition is not cached yet.</param>
    /// <returns>The cached or newly created definition.</returns>
    TDefinition GetOrAdd<TKey, TDefinition>(TKey key, Func<TKey, TDefinition> factory)
        where TKey : notnull
        where TDefinition : ValidationErrorDefinition;
}
