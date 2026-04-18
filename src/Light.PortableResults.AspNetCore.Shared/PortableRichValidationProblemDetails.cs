using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// Schema-only type for OpenAPI documentation. Not used at runtime.
/// Represents a rich Light.PortableResults validation problem details response that documents the
/// <c>errors</c> property as an array of Light.PortableResults-style error objects.
/// Use this type when <c>PortableResultsHttpWriteOptions.ValidationProblemSerializationFormat</c>
/// is set to <c>Rich</c>.
/// </summary>
/// <typeparam name="TErrorMetadata">The type of the metadata on each <see cref="PortableError{TMetadata}" />.</typeparam>
/// <typeparam name="TProblemMetadata">The type of the top-level problem metadata.</typeparam>
public class PortableRichValidationProblemDetails<TErrorMetadata, TProblemMetadata> : ProblemDetails
{
    /// <summary>
    /// Gets or sets the collection of validation errors.
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
/// Convenience non-generic variant of
/// <see cref="PortableRichValidationProblemDetails{TErrorMetadata, TProblemMetadata}" />
/// that uses <see cref="object" /> for both metadata type parameters.
/// </summary>
public class PortableRichValidationProblemDetails : PortableRichValidationProblemDetails<object, object>;
