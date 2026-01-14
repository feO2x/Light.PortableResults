namespace Light.Results;

/// <summary>
/// <para>
/// Represents a void-like successful value.
/// </para>
/// <para>
/// In functional programming, Unit is often used as a placeholder in functions that do not return a value,
/// but still want to use the Result Pattern. This library has the concept of Unit, but allows to use
/// <see cref="Result"/> instead of <c>Result&lt;Unit></c>. Choose whichever you like best.
/// <see cref="Result"/> actually encapsulates a <c>Result&lt;Unit></c> instance.
/// </para>.
/// </summary>
public readonly record struct Unit
{
    /// <summary>
    /// The singleton instance of <see cref="Unit" />.
    /// </summary>
    public static readonly Unit Value = new ();
}
