using System.Net;

// ReSharper disable ConvertToExtensionBlock

namespace Light.PortableResults.Http;

/// <summary>
/// Provides RFC 9110 type URIs and status titles for HTTP status codes. Also provides an extension method to convert
/// <see cref="ErrorCategory"/> to its corresponding HTTP status code.
/// </summary>
public static class ErrorCategoryExtensionsForHttp
{
    /// <summary>
    /// Converts an ErrorCategory to its corresponding HTTP status code.
    /// </summary>
    /// <param name="category">The error category.</param>
    /// <returns>The HTTP status code.</returns>
    public static HttpStatusCode ToHttpStatusCode(this ErrorCategory category) =>
        category == ErrorCategory.Unclassified ? HttpStatusCode.InternalServerError : (HttpStatusCode) category;

    /// <summary>
    /// Gets the RFC 9110 type URI for the specified error category.
    /// </summary>
    /// <param name="category">The error category.</param>
    /// <returns>The RFC 9110 section URI for the category, or the 500 URI for unknown categories.</returns>
    public static string GetTypeUri(this ErrorCategory category) => category switch
    {
        ErrorCategory.Validation => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        ErrorCategory.Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        ErrorCategory.PaymentRequired => "https://tools.ietf.org/html/rfc9110#section-15.5.3",
        ErrorCategory.Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        ErrorCategory.NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        ErrorCategory.MethodNotAllowed => "https://tools.ietf.org/html/rfc9110#section-15.5.6",
        ErrorCategory.NotAcceptable => "https://tools.ietf.org/html/rfc9110#section-15.5.7",
        ErrorCategory.Timeout => "https://tools.ietf.org/html/rfc9110#section-15.5.9",
        ErrorCategory.Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        ErrorCategory.Gone => "https://tools.ietf.org/html/rfc9110#section-15.5.11",
        ErrorCategory.LengthRequired => "https://tools.ietf.org/html/rfc9110#section-15.5.12",
        ErrorCategory.PreconditionFailed => "https://tools.ietf.org/html/rfc9110#section-15.5.13",
        ErrorCategory.ContentTooLarge => "https://tools.ietf.org/html/rfc9110#section-15.5.14",
        ErrorCategory.UriTooLong => "https://tools.ietf.org/html/rfc9110#section-15.5.15",
        ErrorCategory.UnsupportedMediaType => "https://tools.ietf.org/html/rfc9110#section-15.5.16",
        ErrorCategory.RequestedRangeNotSatisfiable => "https://tools.ietf.org/html/rfc9110#section-15.5.17",
        ErrorCategory.ExpectationFailed => "https://tools.ietf.org/html/rfc9110#section-15.5.18",
        ErrorCategory.MisdirectedRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.20",
        ErrorCategory.UnprocessableContent => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
        ErrorCategory.Locked => "https://tools.ietf.org/html/rfc4918#section-11.3",
        ErrorCategory.FailedDependency => "https://tools.ietf.org/html/rfc4918#section-11.4",
        ErrorCategory.UpgradeRequired => "https://tools.ietf.org/html/rfc9110#section-15.5.22",
        ErrorCategory.PreconditionRequired => "https://tools.ietf.org/html/rfc9110#section-15.5.23",
        ErrorCategory.TooManyRequests => "https://tools.ietf.org/html/rfc6585#section-4",
        ErrorCategory.RequestHeaderFieldsTooLarge => "https://tools.ietf.org/html/rfc6585#section-5",
        ErrorCategory.UnavailableForLegalReasons => "https://datatracker.ietf.org/doc/html/rfc7725#section-3",
        ErrorCategory.InternalError => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        ErrorCategory.NotImplemented => "https://tools.ietf.org/html/rfc9110#section-15.6.2",
        ErrorCategory.BadGateway => "https://tools.ietf.org/html/rfc9110#section-15.6.3",
        ErrorCategory.ServiceUnavailable => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
        ErrorCategory.GatewayTimeout => "https://tools.ietf.org/html/rfc9110#section-15.6.5",
        ErrorCategory.InsufficientStorage => "https://tools.ietf.org/html/rfc9110#section-15.6.6",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };

    /// <summary>
    /// Gets the standard title (reason phrase) for the specified error category.
    /// </summary>
    /// <param name="category">The error category.</param>
    /// <returns>The standard title for the category, or "Internal Server Error" for unknown categories.</returns>
    public static string GetTitle(this ErrorCategory category) => category switch
    {
        ErrorCategory.Validation => "Bad Request",
        ErrorCategory.Unauthorized => "Unauthorized",
        ErrorCategory.PaymentRequired => "Payment Required",
        ErrorCategory.Forbidden => "Forbidden",
        ErrorCategory.NotFound => "Not Found",
        ErrorCategory.MethodNotAllowed => "Method Not Allowed",
        ErrorCategory.NotAcceptable => "Not Acceptable",
        ErrorCategory.Timeout => "Request Timeout",
        ErrorCategory.Conflict => "Conflict",
        ErrorCategory.Gone => "Gone",
        ErrorCategory.LengthRequired => "Length Required",
        ErrorCategory.PreconditionFailed => "Precondition Failed",
        ErrorCategory.ContentTooLarge => "Content Too Large",
        ErrorCategory.UriTooLong => "URI Too Long",
        ErrorCategory.UnsupportedMediaType => "Unsupported Media Type",
        ErrorCategory.RequestedRangeNotSatisfiable => "Range Not Satisfiable",
        ErrorCategory.ExpectationFailed => "Expectation Failed",
        ErrorCategory.MisdirectedRequest => "Misdirected Request",
        ErrorCategory.UnprocessableContent => "Unprocessable Entity",
        ErrorCategory.Locked => "Locked",
        ErrorCategory.FailedDependency => "Failed Dependency",
        ErrorCategory.UpgradeRequired => "Upgrade Required",
        ErrorCategory.PreconditionRequired => "Precondition Required",
        ErrorCategory.TooManyRequests => "Too Many Requests",
        ErrorCategory.RequestHeaderFieldsTooLarge => "Request Header Fields Too Large",
        ErrorCategory.UnavailableForLegalReasons => "Unavailable For Legal Reasons",
        ErrorCategory.InternalError => "Internal Server Error",
        ErrorCategory.NotImplemented => "Not Implemented",
        ErrorCategory.BadGateway => "Bad Gateway",
        ErrorCategory.ServiceUnavailable => "Service Unavailable",
        ErrorCategory.GatewayTimeout => "Gateway Timeout",
        ErrorCategory.InsufficientStorage => "Insufficient Storage",
        _ => "Internal Server Error"
    };

    /// <summary>
    /// Gets a human-readable detail message for the specified error category.
    /// </summary>
    /// <param name="category">The error category.</param>
    /// <returns>A descriptive message suitable for the Problem Details "detail" field.</returns>
    public static string GetDetail(this ErrorCategory category) => category switch
    {
        ErrorCategory.Validation => "One or more validation errors occurred.",
        ErrorCategory.Unauthorized => "Authentication is required to access this resource.",
        ErrorCategory.PaymentRequired => "Payment is required before the request can be processed.",
        ErrorCategory.Forbidden => "You do not have permission to access this resource.",
        ErrorCategory.NotFound => "The requested resource was not found.",
        ErrorCategory.MethodNotAllowed => "The HTTP method used is not allowed for this resource.",
        ErrorCategory.NotAcceptable => "No acceptable representation of the resource could be found for the request.",
        ErrorCategory.Timeout => "The request timed out.",
        ErrorCategory.Conflict => "The request conflicts with the current state of the resource.",
        ErrorCategory.Gone => "The requested resource is no longer available.",
        ErrorCategory.LengthRequired => "The request must include a Content-Length header.",
        ErrorCategory.PreconditionFailed => "A precondition for the request was not met.",
        ErrorCategory.ContentTooLarge => "The request content is too large.",
        ErrorCategory.UriTooLong => "The request URI is too long.",
        ErrorCategory.UnsupportedMediaType => "The request content type is not supported.",
        ErrorCategory.RequestedRangeNotSatisfiable => "The requested range cannot be satisfied for this resource.",
        ErrorCategory.ExpectationFailed => "The server could not meet the expectation given in the request headers.",
        ErrorCategory.MisdirectedRequest => "The request was directed to a server that cannot produce a response.",
        ErrorCategory.UnprocessableContent => "The request was well-formed but could not be processed.",
        ErrorCategory.Locked => "The resource is locked and cannot be modified.",
        ErrorCategory.FailedDependency => "The request failed because a dependent request did not succeed.",
        ErrorCategory.UpgradeRequired => "The client must upgrade to a different protocol to complete the request.",
        ErrorCategory.PreconditionRequired => "A required precondition header is missing from the request.",
        ErrorCategory.TooManyRequests => "Too many requests. Please try again later.",
        ErrorCategory.RequestHeaderFieldsTooLarge => "The request headers are too large.",
        ErrorCategory.UnavailableForLegalReasons => "This resource is unavailable for legal reasons.",
        ErrorCategory.InternalError => "An unexpected error occurred.",
        ErrorCategory.NotImplemented => "This functionality is not implemented.",
        ErrorCategory.BadGateway => "An invalid response was received from an upstream server.",
        ErrorCategory.ServiceUnavailable => "The service is currently unavailable.",
        ErrorCategory.GatewayTimeout => "The upstream server did not respond in time.",
        ErrorCategory.InsufficientStorage =>
            "The server cannot store the representation needed to complete the request.",
        _ => "An unexpected error occurred."
    };
}
