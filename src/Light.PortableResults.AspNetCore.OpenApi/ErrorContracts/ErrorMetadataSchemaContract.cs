using System;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Represents a metadata contract backed by an OpenAPI schema factory.
/// </summary>
public sealed class ErrorMetadataSchemaContract : ErrorMetadataContract
{
    /// <summary>
    /// Initializes a new instance of <see cref="ErrorMetadataSchemaContract" />.
    /// </summary>
    /// <param name="schemaFactory">The factory that creates a fresh metadata schema for the requested OpenAPI version.</param>
    /// <param name="schemaId">The schema ID that uniquely identifies this contract. When null, the ID is derived from the factory's method metadata.</param>
    public ErrorMetadataSchemaContract(
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory,
        string? schemaId = null
    )
    {
        ArgumentNullException.ThrowIfNull(schemaFactory);
        SchemaFactory = schemaFactory;
        SchemaId = CreateSchemaId(schemaFactory, schemaId);
    }

    /// <summary>
    /// Gets the factory that creates a fresh metadata schema for the requested OpenAPI version.
    /// </summary>
    public Func<OpenApiSpecVersion, OpenApiSchema> SchemaFactory { get; }

    /// <summary>
    /// Gets the schema ID that uniquely identifies this contract and appears in duplicate-registration errors.
    /// </summary>
    public string SchemaId { get; }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is ErrorMetadataSchemaContract other &&
        string.Equals(SchemaId, other.SchemaId, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode() => SchemaId.GetHashCode(StringComparison.Ordinal);

    private static string CreateSchemaId(
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory,
        string? schemaId
    )
    {
        if (!string.IsNullOrWhiteSpace(schemaId))
        {
            return schemaId;
        }

        var method = schemaFactory.Method;
        var methodName = method.Name;
        var declaringTypeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name;
        if (string.IsNullOrWhiteSpace(methodName) || methodName.Contains('<', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "A schema-based error metadata contract requires a meaningful schema ID. " +
                "Pass the schemaId argument explicitly when registering anonymous or compiler-generated schema factories."
            );
        }

        if (!string.IsNullOrWhiteSpace(declaringTypeName) && !string.IsNullOrWhiteSpace(methodName))
        {
            return $"{declaringTypeName}.{methodName}";
        }

        throw new InvalidOperationException(
            "A schema-based error metadata contract requires a meaningful schema ID. " +
            "Pass the schemaId argument explicitly when registering anonymous or compiler-generated schema factories."
        );
    }
}
