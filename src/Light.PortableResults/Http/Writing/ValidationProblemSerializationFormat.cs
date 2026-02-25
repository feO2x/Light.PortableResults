namespace Light.PortableResults.Http.Writing;

/// <summary>
/// Specifies how validation errors are serialized in Problem Details responses
/// for HTTP 400 Bad Request and HTTP 422 Unprocessable Content responses.
/// For all other error responses, the rich format is always used.
/// </summary>
public enum ValidationProblemSerializationFormat
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
