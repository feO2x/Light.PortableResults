using System;
using System.Collections.Generic;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Represents one documented error entry that can be composed into an OpenAPI response example.
/// </summary>
public sealed class PortableOpenApiErrorExampleEntry : IEquatable<PortableOpenApiErrorExampleEntry>
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortableOpenApiErrorExampleEntry" />.
    /// </summary>
    public PortableOpenApiErrorExampleEntry(
        string code,
        string? target,
        string? message,
        IReadOnlyDictionary<string, object?>? metadata
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code;
        Target = target;
        Message = message;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the validation error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the validation target, or <see langword="null" /> when the target could not be inferred.
    /// </summary>
    public string? Target { get; }

    /// <summary>
    /// Gets the optional example validation message.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the optional error metadata values.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; }

    /// <inheritdoc />
    public bool Equals(PortableOpenApiErrorExampleEntry? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Code, other.Code, StringComparison.Ordinal) &&
               string.Equals(Target, other.Target, StringComparison.Ordinal) &&
               string.Equals(Message, other.Message, StringComparison.Ordinal) &&
               MetadataEquals(Metadata, other.Metadata);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as PortableOpenApiErrorExampleEntry);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Code, StringComparer.Ordinal);
        hashCode.Add(Target, StringComparer.Ordinal);
        hashCode.Add(Message, StringComparer.Ordinal);
        if (Metadata is not null)
        {
            // Equals treats Metadata as an unordered set of key/value pairs, so the hash code must
            // be independent of the dictionary's enumeration order. Combine each pair's hash with a
            // commutative XOR aggregate before folding it into the HashCode.
            var metadataHash = 0;
            foreach (var (key, value) in Metadata)
            {
                metadataHash ^= HashCode.Combine(
                    StringComparer.Ordinal.GetHashCode(key),
                    value?.GetHashCode() ?? 0
                );
            }

            hashCode.Add(metadataHash);
        }

        return hashCode.ToHashCode();
    }

    private static bool MetadataEquals(
        IReadOnlyDictionary<string, object?>? left,
        IReadOnlyDictionary<string, object?>? right
    )
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        foreach (var (key, leftValue) in left)
        {
            if (!right.TryGetValue(key, out var rightValue) || !Equals(leftValue, rightValue))
            {
                return false;
            }
        }

        return true;
    }
}
