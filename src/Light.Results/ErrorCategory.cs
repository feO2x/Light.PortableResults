namespace Light.Results;

/// <summary>
/// Categorizes errors for routing, logging, and HTTP/gRPC status mapping.
/// </summary>
public enum ErrorCategory
{
    /// <summary>Default category when no specific category applies.</summary>
    Unclassified = 0,

    /// <summary>Input validation failures (HTTP 400, gRPC InvalidArgument).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.1</remarks>
    Validation = 400,

    /// <summary>Authentication required or failed (HTTP 401, gRPC Unauthenticated).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.2</remarks>
    Unauthorized = 401,

    /// <summary>Authenticated but not permitted (HTTP 403, gRPC PermissionDenied).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.4</remarks>
    Forbidden = 403,

    /// <summary>Requested resource not found (HTTP 404, gRPC NotFound).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.5</remarks>
    NotFound = 404,

    /// <summary>Request timeout - client took too long to send request (HTTP 408, gRPC DeadlineExceeded).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.9</remarks>
    Timeout = 408,

    /// <summary>Resource state conflict (HTTP 409, gRPC Aborted/AlreadyExists).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.10</remarks>
    Conflict = 409,

    /// <summary>The requested resource is no longer available (HTTP 410).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.11</remarks>
    Gone = 410,

    /// <summary>The requested resource is not in a state that was described by a client precondition (HTTP 412).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.13</remarks>
    PreconditionFailed = 412,

    /// <summary>The content is too large (HTTP 413).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.14</remarks>
    ContentTooLarge = 413,

    /// <summary>The URI is longer than the server is willing to interpret (HTTP 414).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.15</remarks>
    UriTooLong = 414,

    /// <summary>The content is in a format not supported by this method on the target resource (HTTP 415).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.16</remarks>
    UnsupportedMediaType = 415,

    /// <summary>The request is semantically invalid (HTTP 422).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.21</remarks>
    UnprocessableEntity = 422,

    /// <summary>Client has exceeded allowed request rate (HTTP 429, gRPC ResourceExhausted).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc6585#section-4</remarks>
    RateLimited = 429,

    /// <summary>The server is denying access to the resource as a consequence of a legal demand (HTTP 451).</summary>
    /// <remarks>https://datatracker.ietf.org/doc/html/rfc7725#section-3</remarks>
    UnavailableForLegalReasons = 451,

    /// <summary>Unexpected internal error (HTTP 500, gRPC Internal).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.6.1</remarks>
    InternalError = 500,

    /// <summary>
    /// Functionality not implemented (HTTP 501, gRPC Unimplemented). Can be used with canary releases where some
    /// clients are not allowed to access new features.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.6.2</remarks>
    NotImplemented = 501,

    /// <summary>
    /// The server, while acting as a gateway or proxy, received an invalid response from an inbound server it accessed
    /// (HTTP 502).
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.6.3</remarks>
    BadGateway = 502,

    /// <summary>The server is currently unable to handle the request (HTTP 503).</summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.6.4</remarks>
    ServiceUnavailable = 503,

    /// <summary>
    /// The server, while acting as a gateway or proxy, did not receive a timely response from an upstream server
    /// (HTTP 504).
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.6.5</remarks>
    GatewayTimeout = 504
}
