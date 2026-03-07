using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides helpers for normalizing and composing validation target paths.
/// </summary>
public static class ValidationTargets
{
    /// <summary>
    /// Gets the default validation target normalizer instance.
    /// </summary>
    public static IValidationTargetNormalizer DefaultNormalizer { get; } = new DefaultValidationTargetNormalizer();

    /// <summary>
    /// Normalizes the specified raw path with the given normalizer or the built-in default normalizer.
    /// </summary>
    /// <param name="rawPath">The raw path to normalize.</param>
    /// <param name="normalizer">The normalizer to use.</param>
    /// <returns>The normalized target path.</returns>
    public static string Normalize(string rawPath, IValidationTargetNormalizer? normalizer = null) =>
        (normalizer ?? DefaultNormalizer).Normalize(rawPath);

    /// <summary>
    /// Composes a parent prefix and child target using the same flat-path rules as the validation infrastructure.
    /// </summary>
    /// <param name="prefix">The parent prefix. Use an empty string for the root object.</param>
    /// <param name="target">The child target. Use an empty string for the current object.</param>
    /// <returns>The composed validation target.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="prefix" /> or <paramref name="target" /> is <see langword="null" />.
    /// </exception>
    public static string Compose(string prefix, string target)
    {
        if (prefix is null)
        {
            throw new ArgumentNullException(nameof(prefix));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (prefix.Length == 0)
        {
            return target;
        }

        if (target.Length == 0)
        {
            return prefix;
        }

        return target[0] == '[' ? prefix + target : prefix + "." + target;
    }

    /// <summary>
    /// Appends a member segment to the specified prefix.
    /// </summary>
    /// <param name="prefix">The current prefix.</param>
    /// <param name="memberName">The member segment to append.</param>
    /// <returns>The composed target.</returns>
    public static string AppendMember(string prefix, string memberName) => Compose(prefix, memberName);

    /// <summary>
    /// Appends an indexer segment to the specified prefix.
    /// </summary>
    /// <param name="prefix">The current prefix.</param>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The composed target.</returns>
    public static string AppendIndex(string prefix, int index) => Compose(prefix, $"[{index}]");

    internal static bool IsSimpleIdentifier(string rawTarget)
    {
        if (rawTarget.Length == 0)
        {
            return true;
        }

        return rawTarget.IndexOf('.') < 0 && rawTarget.IndexOf('[') < 0 && rawTarget.IndexOf(']') < 0;
    }
}
