namespace Light.Results;

/// <summary>
/// Categorizes errors for routing, logging, and HTTP/gRPC status mapping.
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// Default category when no specific category applies.
    /// </summary>
    Unclassified = 0,

    /// <summary>
    /// Input validation failures (HTTP 400, gRPC InvalidArgument). Indicates that the request could not be understood
    /// by the server. Equivalent to BadRequest in HTTP.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.1</remarks>
    Validation = 400,

    /// <summary>
    /// Authentication required or failed (HTTP 401, gRPC Unauthenticated).
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.2</remarks>
    Unauthorized = 401,

    /// <summary>
    /// Payment required (HTTP 402, gRPC Unauthenticated). Is reserved for future use in HTTP, but actually indicates
    /// that the request requires payment or additional account funds before being processed.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.3</remarks>
    PaymentRequired = 402,

    /// <summary>
    /// Authenticated but not permitted (HTTP 403, gRPC PermissionDenied).
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.4</remarks>
    Forbidden = 403,

    /// <summary>
    /// Requested resource not found (HTTP 404, gRPC NotFound). Indicates that the requested resource does not exist
    /// on the server.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.5</remarks>
    NotFound = 404,

    /// <summary>
    /// Method not allowed (HTTP 405, gRPC Unauthenticated). Indicates that the request method (POST or GET) is not
    /// allowed on the requested resource.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.6</remarks>
    MethodNotAllowed = 405,

    /// <summary>
    /// Not acceptable (HTTP 406, gRPC Unauthenticated). Indicates that the client has indicated with Accept headers
    /// that it will not accept any of the available representations of the resource.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.7</remarks>
    NotAcceptable = 406,

    /// <summary>
    /// Request timeout - client took too long to send request (HTTP 408, gRPC DeadlineExceeded).
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.9</remarks>
    Timeout = 408,

    /// <summary>
    /// Resource state conflict (HTTP 409, gRPC Aborted/AlreadyExists). Indicates that the request could not be carried
    /// out because of a conflict on the server, usually a concurrent change to the same resource.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.10</remarks>
    Conflict = 409,

    /// <summary>
    /// The requested resource is no longer available (HTTP 410). Indicates that the requested resource is no longer
    /// available.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.11</remarks>
    Gone = 410,

    /// <summary>
    /// Length required (HTTP 411, gRPC Unauthenticated). Indicates that the required Content-length header is missing.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.12</remarks>
    LengthRequired = 411,

    /// <summary>
    /// The requested resource is not in a state that was described by a client precondition (HTTP 412).
    /// Indicates that a condition set for this request failed, and the request cannot be carried out.
    /// Conditions are set with conditional request headers like If-Match, If-None-Match, or If-Unmodified-Since in
    /// HTTP.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.13</remarks>
    PreconditionFailed = 412,

    /// <summary>
    /// The content is too large (HTTP 413). Indicates that the request is too large for the server to process.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.14</remarks>
    ContentTooLarge = 413,

    /// <summary>
    /// The URI is longer than the server is willing to interpret (HTTP 414).
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.15</remarks>
    UriTooLong = 414,

    /// <summary>
    /// The content is in a format not supported by this method on the target resource (HTTP 415). Indicates that the
    /// request is an unsupported type.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.16</remarks>
    UnsupportedMediaType = 415,

    /// <summary>
    /// The range specified for the request is not valid (HTTP 416). Indicates that the range of data requested
    /// from the resource cannot be returned. Either because the beginning of the range is before the beginning
    /// of the resource, or the end of the range is after the end of the resource.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.17</remarks>
    RequestedRangeNotSatisfiable = 416,

    /// <summary>
    /// Expectation failed (HTTP 417). Indicates that an expectation given in an Expect header could not be met
    /// by the server.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.18</remarks>
    ExpectationFailed = 417,

    /// <summary>
    /// Misdirected request (HTTP 421). Indicates that the request was directed at a server that is not able to produce
    /// a response (for example, because a target device is offline).
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.20</remarks>
    MisdirectedRequest = 421,

    /// <summary>
    /// The request is semantically invalid (HTTP 422). Indicates that the request was well-formed but was unable to be
    /// followed due to semantic errors. UnprocessableEntity is a synonym for UnprocessableContent.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.21</remarks>
    UnprocessableContent = 422,

    /// <summary>
    /// Locked (HTTP 423). Indicates that the resource is locked and cannot be modified until the lock is released.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc4918#section-11.3</remarks>
    Locked = 423,

    /// <summary>
    /// Failed dependency (HTTP 424). Indicates that the method could not be completed due to a failure of a previous
    /// request (e.g., a precondition check failed).
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc4918#section-11.4</remarks>
    FailedDependency = 424,

    /// <summary>
    /// Upgrade required (HTTP 426). Indicates that the server requires that the client upgrade to a different protocol
    /// (e.g., from HTTP/1.1 to HTTP/2).
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.22</remarks>
    UpgradeRequired = 426,

    /// <summary>
    /// Precondition required (HTTP 428). Indicates that the server requires that the client include a precondition
    /// (e.g., an If-Match or If-None-Match header) in the request.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.5.23</remarks>
    PreconditionRequired = 428,

    /// <summary>
    /// Client has exceeded allowed request rate (HTTP 429, gRPC ResourceExhausted). Indicates that the user has sent
    /// too many requests in a given amount of time.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc6585#section-4</remarks>
    TooManyRequests = 429,

    /// <summary>
    /// Request header fields too large (HTTP 431). Indicates that the request header fields are too large, either one
    /// individual field or the total size of all fields.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc6585#section-5</remarks>
    RequestHeaderFieldsTooLarge = 431,

    /// <summary>
    /// The server is denying access to the resource as a consequence of a legal demand (HTTP 451).
    /// </summary>
    /// <remarks>https://datatracker.ietf.org/doc/html/rfc7725#section-3</remarks>
    UnavailableForLegalReasons = 451,

    /// <summary>
    /// Unexpected internal error (HTTP 500, gRPC Internal).
    /// </summary>
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

    /// <summary>
    /// The server is currently unable to handle the request (HTTP 503).
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.6.4</remarks>
    ServiceUnavailable = 503,

    /// <summary>
    /// The server, while acting as a gateway or proxy, did not receive a timely response from an upstream server
    /// (HTTP 504).
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.6.5</remarks>
    GatewayTimeout = 504,

    /// <summary>
    /// Insufficient storage (HTTP 507). Indicates that the server is unable to store the representation needed to
    /// complete the request.
    /// </summary>
    /// <remarks>https://tools.ietf.org/html/rfc9110#section-15.6.6</remarks>
    InsufficientStorage = 507
}
