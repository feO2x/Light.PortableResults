namespace Light.Results.AspNetCore.Shared;

/// <summary>
/// Provides RFC 9110 type URIs and status titles for HTTP status codes.
/// </summary>
public static class HttpStatusCodeInfo
{
    public static string GetTypeUri(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        408 => "https://tools.ietf.org/html/rfc9110#section-15.5.9",
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        410 => "https://tools.ietf.org/html/rfc9110#section-15.5.11",
        412 => "https://tools.ietf.org/html/rfc9110#section-15.5.13",
        413 => "https://tools.ietf.org/html/rfc9110#section-15.5.14",
        414 => "https://tools.ietf.org/html/rfc9110#section-15.5.15",
        415 => "https://tools.ietf.org/html/rfc9110#section-15.5.16",
        422 => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
        429 => "https://tools.ietf.org/html/rfc6585#section-4",
        451 => "https://datatracker.ietf.org/doc/html/rfc7725#section-3",
        500 => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        501 => "https://tools.ietf.org/html/rfc9110#section-15.6.2",
        502 => "https://tools.ietf.org/html/rfc9110#section-15.6.3",
        503 => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
        504 => "https://tools.ietf.org/html/rfc9110#section-15.6.5",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };

    public static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        408 => "Request Timeout",
        409 => "Conflict",
        410 => "Gone",
        412 => "Precondition Failed",
        413 => "Content Too Large",
        414 => "URI Too Long",
        415 => "Unsupported Media Type",
        422 => "Unprocessable Entity",
        429 => "Too Many Requests",
        451 => "Unavailable For Legal Reasons",
        500 => "Internal Server Error",
        501 => "Not Implemented",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        504 => "Gateway Timeout",
        _ => "Internal Server Error"
    };

    public static string GetDetail(ErrorCategory category) => category switch
    {
        ErrorCategory.Validation => "One or more validation errors occurred.",
        ErrorCategory.Unauthorized => "Authentication is required to access this resource.",
        ErrorCategory.Forbidden => "You do not have permission to access this resource.",
        ErrorCategory.NotFound => "The requested resource was not found.",
        ErrorCategory.Timeout => "The request timed out.",
        ErrorCategory.Conflict => "The request conflicts with the current state of the resource.",
        ErrorCategory.Gone => "The requested resource is no longer available.",
        ErrorCategory.PreconditionFailed => "A precondition for the request was not met.",
        ErrorCategory.ContentTooLarge => "The request content is too large.",
        ErrorCategory.UriTooLong => "The request URI is too long.",
        ErrorCategory.UnsupportedMediaType => "The request content type is not supported.",
        ErrorCategory.UnprocessableEntity => "The request was well-formed but could not be processed.",
        ErrorCategory.RateLimited => "Too many requests. Please try again later.",
        ErrorCategory.UnavailableForLegalReasons => "This resource is unavailable for legal reasons.",
        ErrorCategory.InternalError => "An unexpected error occurred.",
        ErrorCategory.NotImplemented => "This functionality is not implemented.",
        ErrorCategory.BadGateway => "An invalid response was received from an upstream server.",
        ErrorCategory.ServiceUnavailable => "The service is currently unavailable.",
        ErrorCategory.GatewayTimeout => "The upstream server did not respond in time.",
        _ => "An unexpected error occurred."
    };
}
