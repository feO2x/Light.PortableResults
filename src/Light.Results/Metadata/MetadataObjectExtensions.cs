using System;

namespace Light.Results.Metadata;

/// <summary>
/// Extension methods for <see cref="MetadataObject" /> including merge operations.
/// </summary>
public static class MetadataObjectExtensions
{
    /// <summary>
    /// Merges two <see cref="MetadataObject" /> instances according to the specified strategy.
    /// </summary>
    public static MetadataObject Merge(
        this MetadataObject original,
        MetadataObject incoming,
        MetadataMergeStrategy strategy = MetadataMergeStrategy.AddOrReplace
    )
    {
        if (incoming.Count == 0)
        {
            return original;
        }

        if (original.Count == 0)
        {
            return incoming;
        }

        using var builder = MetadataObjectBuilder.From(original);

        foreach (var kvp in incoming)
        {
            var key = kvp.Key;
            var incomingValue = kvp.Value;

            if (!builder.TryGetValue(key, out var existingValue))
            {
                builder.Add(key, incomingValue);
                continue;
            }

            switch (strategy)
            {
                case MetadataMergeStrategy.AddOrReplace:
                    builder.Replace(key, MergeValues(existingValue, incomingValue, strategy));
                    break;

                case MetadataMergeStrategy.PreserveExisting:
                    // Keep existing, do nothing
                    break;

                case MetadataMergeStrategy.FailOnConflict:
                    throw new InvalidOperationException($"Duplicate metadata key '{key}'.");

                default:
                    throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown merge strategy.");
            }
        }

        return builder.Build();
    }

    private static MetadataValue MergeValues(
        MetadataValue left,
        MetadataValue right,
        MetadataMergeStrategy strategy
    )
    {
        if (left.Kind == MetadataKind.Object &&
            right.Kind == MetadataKind.Object &&
            left.TryGetObject(out var leftObj) &&
            right.TryGetObject(out var rightObj))
        {
            return MetadataValue.FromObject(leftObj.Merge(rightObj, strategy));
        }

        // Scalars and arrays are replaced wholesale
        return right;
    }

    /// <summary>
    /// Creates a new <see cref="MetadataObject" /> with an additional property.
    /// </summary>
    public static MetadataObject With(this MetadataObject obj, string key, MetadataValue value)
    {
        using var builder = MetadataObjectBuilder.From(obj);
        builder.AddOrReplace(key, value);
        return builder.Build();
    }

    /// <summary>
    /// Creates a new <see cref="MetadataObject" /> with additional properties.
    /// </summary>
    public static MetadataObject With(this MetadataObject obj, params (string Key, MetadataValue Value)[]? properties)
    {
        if (properties is null || properties.Length == 0)
        {
            return obj;
        }

        using var builder = MetadataObjectBuilder.From(obj);
        foreach (var (key, value) in properties)
        {
            builder.AddOrReplace(key, value);
        }

        return builder.Build();
    }
}
