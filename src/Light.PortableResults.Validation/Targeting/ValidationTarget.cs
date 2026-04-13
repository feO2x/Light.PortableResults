using System;

namespace Light.PortableResults.Validation.Targeting;

/// <summary>
/// Describes how a validation target input must be interpreted and whether it is already normalized.
/// </summary>
public readonly record struct ValidationTarget
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationTarget" />.
    /// </summary>
    /// <param name="input">The target input text.</param>
    /// <param name="semantics">The semantics used to interpret <paramref name="input" />.</param>
    /// <param name="isNormalized">
    /// <see langword="true" /> when <paramref name="input" /> is already normalized for the specified
    /// <paramref name="semantics" />; otherwise, <see langword="false" />.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input" /> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="semantics" /> is not supported.</exception>
    public ValidationTarget(string input, ValidationTargetSemantics semantics, bool isNormalized = false)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        if (!semantics.IsDefined())
        {
            throw new ArgumentOutOfRangeException(nameof(semantics));
        }

        Semantics = semantics;
        IsNormalized = isNormalized;
    }

    /// <summary>
    /// Gets a value indicating whether this instance is the default value.
    /// </summary>
    public bool IsDefault => Input is null;

    /// <summary>
    /// Gets the target input text.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Gets the semantics used to interpret <see cref="Input" />.
    /// </summary>
    public ValidationTargetSemantics Semantics { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Input" /> is already normalized for <see cref="Semantics" />.
    /// </summary>
    public bool IsNormalized { get; }

    /// <summary>
    /// Creates a caller-expression target.
    /// </summary>
    public static ValidationTarget CallerExpression(string input, bool isNormalized = false) =>
        new (input, ValidationTargetSemantics.CallerExpression, isNormalized);

    /// <summary>
    /// Creates a relative validation target.
    /// </summary>
    public static ValidationTarget Relative(string input, bool isNormalized = false) =>
        new (input, ValidationTargetSemantics.Relative, isNormalized);

    /// <summary>
    /// Creates an absolute validation target.
    /// </summary>
    public static ValidationTarget Absolute(string input, bool isNormalized = false) =>
        new (input, ValidationTargetSemantics.Absolute, isNormalized);
}
