using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// Schema-only type for OpenAPI documentation. Not used at runtime.
/// Represents an ASP.NET Core-compatible Light.PortableResults validation problem details response.
/// The inherited <c>Errors</c> property from <see cref="HttpValidationProblemDetails" /> is documented
/// as <c>Dictionary&lt;string, string[]&gt;</c>; the optional <see cref="ErrorDetails" /> array carries
/// Light.PortableResults-specific information such as codes, categories, and metadata.
/// Use this type when <c>PortableResultsHttpWriteOptions.ValidationProblemSerializationFormat</c>
/// is set to <c>AspNetCoreCompatible</c>.
/// </summary>
/// <typeparam name="TErrorDetailMetadata">The type of the metadata on each error details entry.</typeparam>
/// <typeparam name="TProblemMetadata">The type of the top-level problem metadata.</typeparam>
public class PortableAspNetCoreValidationProblemDetails<TErrorDetailMetadata, TProblemMetadata>
    : HttpValidationProblemDetails
{
    /// <summary>
    /// Gets or sets the optional Light.PortableResults-specific error details that correlate with
    /// the inherited <c>errors</c> dictionary.
    /// </summary>
    public IReadOnlyList<PortableValidationErrorDetail<TErrorDetailMetadata>>? ErrorDetails { get; init; }

    /// <summary>
    /// Gets or sets the top-level problem metadata.
    /// </summary>
    public TProblemMetadata Metadata { get; init; } = default!;
}

/// <summary>
/// Schema-only type for OpenAPI documentation. Not used at runtime.
/// Convenience non-generic variant of
/// <see cref="PortableAspNetCoreValidationProblemDetails{TErrorDetailMetadata, TProblemMetadata}" />
/// that uses <see cref="object" /> for both metadata type parameters.
/// </summary>
public class PortableAspNetCoreValidationProblemDetails
    : PortableAspNetCoreValidationProblemDetails<object, object>;
