using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Represents a typed key for shared validation context items.
/// </summary>
/// <typeparam name="T">The associated item type.</typeparam>
public sealed record ValidationContextKey<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationContextKey{T}" /> class.
    /// </summary>
    /// <param name="name">A diagnostic name for the key.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is empty or whitespace.</exception>
    public ValidationContextKey(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The key name must not be empty or whitespace.", nameof(name));
        }

        Name = name;
    }

    /// <summary>
    /// Gets the diagnostic name of the key.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public override string ToString() => Name;
}
