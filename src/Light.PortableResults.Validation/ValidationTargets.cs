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
    /// Creates a caller-expression validation target.
    /// </summary>
    public static ValidationTarget CallerExpression(string input, bool isNormalized = false) =>
        ValidationTarget.CallerExpression(input, isNormalized);

    /// <summary>
    /// Creates a relative validation target.
    /// </summary>
    public static ValidationTarget Relative(string input, bool isNormalized = false) =>
        ValidationTarget.Relative(input, isNormalized);

    /// <summary>
    /// Creates an absolute validation target.
    /// </summary>
    public static ValidationTarget Absolute(string input, bool isNormalized = false) =>
        ValidationTarget.Absolute(input, isNormalized);

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
    /// Appends an indexer segment to the specified prefix.
    /// </summary>
    /// <param name="prefix">The current prefix.</param>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The composed target.</returns>
    public static string AppendIndex(string prefix, int index) => Compose(prefix, $"[{index}]");

    /// <summary>
    /// Gets a value indicating whether the specified raw target is a simple identifier.
    /// This is true when the target does not contain any dots or square brackets.
    /// </summary>
    /// <param name="rawTarget">The raw target to check.</param>
    /// <returns><see langword="true" /> when the target is a simple identifier; otherwise, <see langword="false" />.</returns>
    public static bool IsSimpleIdentifier(string rawTarget)
    {
        if (rawTarget.Length == 0)
        {
            return true;
        }

        return rawTarget.IndexOf('.') < 0 && rawTarget.IndexOf('[') < 0 && rawTarget.IndexOf(']') < 0;
    }
}
