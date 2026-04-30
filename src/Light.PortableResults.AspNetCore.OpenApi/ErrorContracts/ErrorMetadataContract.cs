using System;
using System.Runtime.CompilerServices;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Represents a documented metadata contract for a portable error code.
/// </summary>
public abstract class ErrorMetadataContract
{
    private protected ErrorMetadataContract() { }

    /// <summary>
    /// Gets the singleton contract for error codes that do not emit metadata.
    /// </summary>
    public static ErrorMetadataContract NoMetadata { get; } = new NoMetadataContract();

    /// <summary>
    /// Creates a contract backed by a CLR metadata type.
    /// </summary>
    /// <param name="metadataType">The CLR metadata type.</param>
    /// <returns>The metadata contract.</returns>
    public static ErrorMetadataContract FromType(Type metadataType)
    {
        ArgumentNullException.ThrowIfNull(metadataType);
        return new ErrorMetadataTypeContract(metadataType);
    }

    /// <summary>
    /// Creates a contract backed by a schema factory.
    /// </summary>
    /// <param name="schemaFactory">The factory that creates a fresh metadata schema for the requested OpenAPI version.</param>
    /// <param name="diagnosticName">The optional diagnostic name used in duplicate-contract errors.</param>
    /// <returns>The metadata contract.</returns>
    public static ErrorMetadataContract FromSchema(
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory,
        [CallerArgumentExpression(nameof(schemaFactory))] string? diagnosticName = null
    )
    {
        ArgumentNullException.ThrowIfNull(schemaFactory);
        return new ErrorMetadataSchemaContract(schemaFactory, diagnosticName);
    }
}
