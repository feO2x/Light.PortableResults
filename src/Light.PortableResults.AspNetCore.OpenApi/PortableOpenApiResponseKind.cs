namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Identifies the documented Light.PortableResults HTTP response shape.
/// </summary>
public enum PortableOpenApiResponseKind
{
    /// <summary>
    /// A successful response that may serialize a bare value or a value-plus-metadata envelope.
    /// </summary>
    SuccessResponse = 0,

    /// <summary>
    /// A non-validation RFC 9457 problem details response.
    /// </summary>
    Problem = 1,

    /// <summary>
    /// A validation problem details response.
    /// </summary>
    ValidationProblem = 2
}
