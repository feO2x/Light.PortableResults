using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// Schema-only type for OpenAPI documentation. Not used at runtime.
/// Represents a rich Light.PortableResults problem details response for non-validation failures,
/// with strongly typed per-error metadata and top-level problem metadata.
/// </summary>
/// <typeparam name="TErrorMetadata">The type of the metadata on each <see cref="PortableError{TMetadata}" />.</typeparam>
/// <typeparam name="TProblemMetadata">The type of the top-level problem metadata.</typeparam>
public class PortableProblemDetails<TErrorMetadata, TProblemMetadata> : ProblemDetails
{
    /// <summary>
    /// Gets or sets the collection of errors that caused the failure.
    /// </summary>
    public IReadOnlyList<PortableError<TErrorMetadata>> Errors { get; init; } =
        new List<PortableError<TErrorMetadata>>();

    /// <summary>
    /// Gets or sets the top-level problem metadata.
    /// </summary>
    public TProblemMetadata Metadata { get; init; } = default!;
}

/// <summary>
/// Schema-only type for OpenAPI documentation. Not used at runtime.
/// Convenience non-generic variant of <see cref="PortableProblemDetails{TErrorMetadata, TProblemMetadata}" />
/// that uses <see cref="object" /> for both metadata type parameters.
/// </summary>
public class PortableProblemDetails : PortableProblemDetails<object, object>;
