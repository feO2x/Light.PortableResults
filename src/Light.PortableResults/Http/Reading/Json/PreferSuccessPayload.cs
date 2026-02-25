namespace Light.PortableResults.Http.Reading.Json;

/// <summary>
/// Specifies how successful result payloads should be interpreted.
/// </summary>
public enum PreferSuccessPayload
{
    /// <summary>
    /// Automatically detect wrapper payloads; treat as wrapper only when the root object contains
    /// only the value and optional metadata properties.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Always interpret the payload as the bare value.
    /// </summary>
    BareValue = 1,

    /// <summary>
    /// Always interpret the payload as a wrapper containing value and optional metadata.
    /// </summary>
    WrappedValue = 2
}
