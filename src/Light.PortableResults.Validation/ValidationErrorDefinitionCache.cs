using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Light.PortableResults.Validation;

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

    private sealed class CacheBucket<TKey, TDefinition>
        where TKey : notnull
        where TDefinition : ValidationErrorDefinition
    {
        private readonly ConcurrentDictionary<TKey, TDefinition> _definitions = new (EqualityComparer<TKey>.Default);

        public TDefinition GetOrAdd(TKey key, Func<TKey, TDefinition> factory) => _definitions.GetOrAdd(key, factory);
    }
}
