using System.Runtime.CompilerServices;

namespace Light.PortableResults.Validation.Normalization;

/// <summary>
/// Trims incoming string values and converts <see langword="null" /> to an empty string.
/// For non-string types, the value is returned unchanged. When the value is typed as
/// <see langword="object" /> and is a non-null string, the string is trimmed.
/// </summary>
public sealed class TrimStringNormalizer : IValueNormalizer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrimStringNormalizer" /> class.
    /// </summary>
    public TrimStringNormalizer() { }

    /// <summary>
    /// Gets the shared singleton instance.
    /// </summary>
    public static TrimStringNormalizer Instance { get; } = new ();

    /// <inheritdoc />
    public T Normalize<T>(T value)
    {
        // Branch 1: JIT removes this for any T != string at specialization time.
        if (typeof(T) == typeof(string))
        {
            var valueAsString = Unsafe.As<T, string?>(ref value);
            var result = valueAsString?.Trim() ?? string.Empty;
            return Unsafe.As<string, T>(ref result);
        }

        // Branch 2: runtime pattern match — fires when T is object and value is a non-null string.
        // Null objects do not match the pattern and fall through to the return below.
        if (value is string stringValue)
        {
            var trimmed = stringValue.Trim();
            return Unsafe.As<string, T>(ref trimmed);
        }

        return value;
    }
}
