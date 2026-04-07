using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation.Caching;

// See the comment at the top of BuiltInValidationErrorDefinitions.cs for the rationale behind using small immutable
// value-type keys on the built-in definition path. This cache stays generic so callers can still bring their own key
// type, but the built-in rules deliberately avoid per-lookup key allocations.

/// <summary>
/// Default thread-safe cache for immutable validation error definitions.
/// </summary>
public sealed class ValidationErrorDefinitionCache : IValidationErrorDefinitionCache
{
    private readonly ConcurrentDictionary<CacheBucketKey, object> _buckets = new ();

    /// <summary>
    /// Gets the shared singleton cache instance.
    /// </summary>
    public static ValidationErrorDefinitionCache Default { get; } = new ();

    /// <inheritdoc />
    public TDefinition GetOrAdd<TKey, TDefinition>(TKey key, Func<TKey, TDefinition> factory)
        where TKey : notnull
        where TDefinition : ValidationErrorDefinition
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        var bucketKey = new CacheBucketKey(typeof(TKey), typeof(TDefinition));
        var bucket = (CacheBucket<TKey, TDefinition>) _buckets.GetOrAdd(
            bucketKey,
            static _ => new CacheBucket<TKey, TDefinition>()
        );
        return bucket.GetOrAdd(key, factory);
    }

    private readonly struct CacheBucketKey : IEquatable<CacheBucketKey>
    {
        public CacheBucketKey(Type keyType, Type definitionType)
        {
            KeyType = keyType;
            DefinitionType = definitionType;
        }

        private Type KeyType { get; }
        private Type DefinitionType { get; }

        public bool Equals(CacheBucketKey other) =>
            KeyType == other.KeyType && DefinitionType == other.DefinitionType;

        public override bool Equals(object? obj) => obj is CacheBucketKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(KeyType, DefinitionType);
    }

    // The extra CacheBucket<TKey, TDefinition> layer exists so that the hot path can stay strongly typed after the first
    // bucket lookup. The outer dictionary only maps a (key type, definition type) pair to the corresponding typed bucket.
    // The real caching work then happens inside a ConcurrentDictionary<TKey, TDefinition>, which avoids boxing struct keys,
    // uses EqualityComparer<TKey>.Default directly, and returns the definition with its concrete type instead of forcing
    // the whole cache through object-based keys and values.
    //
    // The inner store also has to be thread-safe. The cache is a shared singleton across validation runs, so once a
    // bucket has been created, multiple threads can still call GetOrAdd on that same bucket concurrently. A normal
    // Dictionary<TKey, TValue> would therefore need explicit locking around every lookup/add operation. Using
    // ConcurrentDictionary<TKey, TDefinition> keeps that logic simple and gives us the atomic GetOrAdd semantics that this
    // cache needs.
    private sealed class CacheBucket<TKey, TDefinition>
        where TKey : notnull
        where TDefinition : ValidationErrorDefinition
    {
        private readonly ConcurrentDictionary<TKey, TDefinition> _definitions = new (EqualityComparer<TKey>.Default);

        public TDefinition GetOrAdd(TKey key, Func<TKey, TDefinition> factory) => _definitions.GetOrAdd(key, factory);
    }
}
