namespace Light.Results.AspNetCore.Shared;

/// <summary>
/// Specifies how errors are serialized in Problem Details responses.
/// </summary>
public enum ErrorSerializationFormat
{
    /// <summary>
    /// ASP.NET Core-compatible: errors as Dictionary&lt;string, string[]&gt; grouped by target.
    /// Additional error details in separate "errorDetails" array when present.
    /// </summary>
    AspNetCoreCompatible,

    /// <summary>
    /// Rich format: errors as array of objects with all properties inline.
    /// </summary>
    Rich
}
