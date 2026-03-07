namespace Light.PortableResults.Validation;

/// <summary>
/// Specifies how member path segments are cased by the built-in validation target normalizer.
/// </summary>
public enum ValidationTargetCasing
{
    /// <summary>
    /// Converts member segments to lower camel case.
    /// </summary>
    CamelCase,

    /// <summary>
    /// Converts member segments to upper camel case.
    /// </summary>
    PascalCase,

    /// <summary>
    /// Preserves the original segment casing.
    /// </summary>
    Preserve
}
