namespace Light.Results.Http.Reading;

/// <summary>
/// Specifies how header values are parsed when no custom
/// <see cref="Light.Results.Http.Reading.Headers.HttpHeaderParser" /> is registered.
/// </summary>
public enum HeaderValueParsingMode
{
    /// <summary>
    /// Preserve header values as strings.
    /// </summary>
    StringOnly,

    /// <summary>
    /// Parse header values as bool, int64, or double when possible; otherwise preserve as string.
    /// </summary>
    Primitive
}
